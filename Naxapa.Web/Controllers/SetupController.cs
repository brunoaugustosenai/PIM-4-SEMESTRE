using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Entities;
using Naxapa.Web.Models;
using Naxapa.Web.Security;
using Naxapa.Web.Services;

namespace Naxapa.Web.Controllers;

[AllowAnonymous]
public sealed class SetupController(
    NaxapaDbContext db,
    IPasswordHasher<UsuarioEntity> passwordHasher,
    UserAuthenticationService authenticationService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (await db.Usuarios.AsNoTracking().AnyAsync(cancellationToken))
            return RedirectToAction("Login", "Conta");

        if (!SecurityHelpers.IsLoopback(HttpContext))
            return StatusCode(StatusCodes.Status403Forbidden, "A configuração inicial deve ser feita no computador servidor usando localhost.");

        var existing = await db.Estabelecimentos.AsNoTracking().OrderBy(x => x.EstabelecimentoId).FirstOrDefaultAsync(cancellationToken);
        return View(new SetupAdminViewModel
        {
            Estabelecimento = existing?.NomeFantasia == "NAXAPA - Estabelecimento" ? string.Empty : existing?.NomeFantasia ?? string.Empty,
            CodigoEstabelecimento = existing?.Codigo,
            Documento = existing?.Documento ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SetupAdminViewModel model, CancellationToken cancellationToken)
    {
        if (!SecurityHelpers.IsLoopback(HttpContext))
            return StatusCode(StatusCodes.Status403Forbidden, "A configuração inicial deve ser feita no computador servidor usando localhost.");

        if (await db.Usuarios.AsNoTracking().AnyAsync(cancellationToken))
            return RedirectToAction("Login", "Conta");

        var documento = BrazilianDocumentValidator.DigitsOnly(model.Documento);
        var bootstrapEstablishmentId = await db.Estabelecimentos.AsNoTracking()
            .OrderBy(x => x.EstabelecimentoId)
            .Select(x => (long?)x.EstabelecimentoId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!BrazilianDocumentValidator.IsValidCpfOrCnpj(documento))
            ModelState.AddModelError(nameof(model.Documento), "Informe um CPF ou CNPJ válido.");
        else if (await db.Estabelecimentos.AsNoTracking().AnyAsync(
                     x => x.Documento == documento && x.EstabelecimentoId != bootstrapEstablishmentId, cancellationToken))
            ModelState.AddModelError(nameof(model.Documento), "Este CPF/CNPJ já está cadastrado em outro estabelecimento.");

        var normalizedUser = SecurityHelpers.NormalizeIdentity(model.UserName);
        var normalizedEmail = SecurityHelpers.NormalizeIdentity(model.Email);
        if (await db.Usuarios.AnyAsync(x => x.NormalizedUserName == normalizedUser || x.NormalizedEmail == normalizedEmail, cancellationToken))
            ModelState.AddModelError(string.Empty, "Usuário ou e-mail já cadastrado.");

        if (!ModelState.IsValid)
            return View(model);

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var establishment = await db.Estabelecimentos.OrderBy(x => x.EstabelecimentoId).FirstOrDefaultAsync(cancellationToken);
        if (establishment is null)
        {
            establishment = new EstabelecimentoEntity
            {
                Codigo = Clean(model.CodigoEstabelecimento),
                NomeFantasia = model.Estabelecimento.Trim(),
                Documento = documento,
                Ativo = true,
                CriadoEm = DateTime.UtcNow
            };
            db.Estabelecimentos.Add(establishment);
        }
        else
        {
            establishment.Codigo = Clean(model.CodigoEstabelecimento);
            establishment.NomeFantasia = model.Estabelecimento.Trim();
            establishment.Documento = documento;
            establishment.Ativo = true;
            establishment.AtualizadoEm = DateTime.UtcNow;
        }

        var user = new UsuarioEntity
        {
            Nome = model.Nome.Trim(),
            UserName = model.UserName.Trim(),
            NormalizedUserName = normalizedUser,
            Email = model.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            EmailConfirmado = true,
            Ativo = true,
            BloqueioHabilitado = true,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            CriadoEm = DateTime.UtcNow
        };
        user.SenhaHash = passwordHasher.HashPassword(user, model.Password);
        db.Usuarios.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var adminEst = await db.Perfis.FirstOrDefaultAsync(x => x.Nome == "Administrador do Estabelecimento", cancellationToken)
            ?? throw new InvalidOperationException("Perfil Administrador do Estabelecimento não encontrado.");
        var adminGlobal = await db.Perfis.FirstOrDefaultAsync(x => x.Nome == "Administrador Geral", cancellationToken)
            ?? throw new InvalidOperationException("Perfil Administrador Geral não encontrado.");

        var link = new UsuarioEstabelecimentoEntity
        {
            UsuarioId = user.UsuarioId,
            EstabelecimentoId = establishment.EstabelecimentoId,
            Principal = true,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };
        db.UsuarioEstabelecimentos.Add(link);
        await db.SaveChangesAsync(cancellationToken);

        db.UsuarioEstabelecimentoPerfis.Add(new UsuarioEstabelecimentoPerfilEntity
        {
            UsuarioEstabelecimentoId = link.UsuarioEstabelecimentoId,
            PerfilId = adminEst.PerfilId
        });
        db.UsuarioPerfisGlobais.Add(new UsuarioPerfilGlobalEntity
        {
            UsuarioId = user.UsuarioId,
            PerfilId = adminGlobal.PerfilId
        });
        await db.SaveChangesAsync(cancellationToken);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC cad.sp_Estabelecimento_Inicializar @EstabelecimentoId={establishment.EstabelecimentoId}",
            cancellationToken);

        if (!await db.Cardapios.AnyAsync(x => x.EstabelecimentoId == establishment.EstabelecimentoId, cancellationToken))
        {
            db.Cardapios.Add(new CardapioEntity
            {
                EstabelecimentoId = establishment.EstabelecimentoId,
                Nome = "Cardápio Principal",
                Descricao = "Cardápio principal do estabelecimento",
                Publicado = false,
                Ativo = true,
                CriadoEm = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);

        var principal = await authenticationService.CreatePrincipalForUserAsync(user.UsuarioId, establishment.EstabelecimentoId, cancellationToken);
        await HttpContext.SignInAsync(NaxapaAuthenticationSchemes.Cookie, principal, new AuthenticationProperties
        {
            IsPersistent = false,
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });

        TempData["Success"] = "Configuração inicial concluída. O administrador foi criado com segurança.";
        return RedirectToAction("Index", "Home");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
