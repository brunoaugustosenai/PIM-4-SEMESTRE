using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Entities;
using Naxapa.Web.Models;
using Naxapa.Web.Security;

namespace Naxapa.Web.Controllers;

[Authorize]
public sealed class EstabelecimentosController(NaxapaDbContext db, CurrentUserService currentUser) : Controller
{
    [NaxapaPermission("ESTABELECIMENTO.VISUALIZAR")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var global = await currentUser.IsGlobalAdminAsync(cancellationToken);
        var query = db.Estabelecimentos.AsNoTracking();
        if (!global)
        {
            var id = currentUser.EstabelecimentoId;
            query = query.Where(x => x.EstabelecimentoId == id);
        }

        var rows = await query.OrderBy(x => x.NomeFantasia)
            .Select(x => new EstablishmentRow(
                x.EstabelecimentoId,
                x.Codigo ?? "-",
                x.NomeFantasia,
                x.Documento ?? "-",
                x.Cidade ?? "-",
                x.UF ?? "-",
                x.Ativo))
            .ToListAsync(cancellationToken);
        return View(rows);
    }

    [NaxapaPermission("ESTABELECIMENTO.EDITAR")]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        if (!await currentUser.IsGlobalAdminAsync(cancellationToken))
            return Forbid();
        return View(new EstablishmentFormViewModel());
    }

    [NaxapaPermission("ESTABELECIMENTO.EDITAR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EstablishmentFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await currentUser.IsGlobalAdminAsync(cancellationToken))
            return Forbid();
        Normalize(model);
        await ValidateAsync(model, null, cancellationToken);
        if (!ModelState.IsValid)
            return View(model);

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        var entity = ToEntity(model, new EstabelecimentoEntity { CriadoEm = DateTime.UtcNow });
        db.Estabelecimentos.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        var adminProfileId = await db.Perfis.Where(x => x.Nome == "Administrador do Estabelecimento")
            .Select(x => x.PerfilId).SingleAsync(cancellationToken);
        var link = new UsuarioEstabelecimentoEntity
        {
            UsuarioId = currentUser.UserId,
            EstabelecimentoId = entity.EstabelecimentoId,
            Principal = false,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };
        db.UsuarioEstabelecimentos.Add(link);
        await db.SaveChangesAsync(cancellationToken);
        db.UsuarioEstabelecimentoPerfis.Add(new UsuarioEstabelecimentoPerfilEntity
        {
            UsuarioEstabelecimentoId = link.UsuarioEstabelecimentoId,
            PerfilId = adminProfileId
        });
        await db.SaveChangesAsync(cancellationToken);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC cad.sp_Estabelecimento_Inicializar @EstabelecimentoId={entity.EstabelecimentoId}", cancellationToken);
        db.Cardapios.Add(new CardapioEntity
        {
            EstabelecimentoId = entity.EstabelecimentoId,
            Nome = "Cardápio Principal",
            Publicado = false,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        TempData["Success"] = "Estabelecimento cadastrado.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("ESTABELECIMENTO.EDITAR")]
    [HttpGet]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        if (!await currentUser.CanAccessEstablishmentAsync(id, cancellationToken))
            return Forbid();
        var e = await db.Estabelecimentos.AsNoTracking().FirstOrDefaultAsync(x => x.EstabelecimentoId == id, cancellationToken);
        if (e is null) return NotFound();
        return View(new EstablishmentFormViewModel
        {
            EstabelecimentoId = e.EstabelecimentoId,
            Codigo = e.Codigo,
            NomeFantasia = e.NomeFantasia,
            RazaoSocial = e.RazaoSocial,
            Documento = e.Documento,
            InscricaoEstadual = e.InscricaoEstadual,
            Telefone = e.Telefone,
            Email = e.Email,
            Logradouro = e.Logradouro,
            Numero = e.Numero,
            Complemento = e.Complemento,
            Bairro = e.Bairro,
            Cidade = e.Cidade,
            UF = e.UF,
            CEP = e.CEP,
            HoraAbertura = e.HoraAbertura,
            HoraFechamento = e.HoraFechamento,
            Ativo = e.Ativo
        });
    }

    [NaxapaPermission("ESTABELECIMENTO.EDITAR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, EstablishmentFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await currentUser.CanAccessEstablishmentAsync(id, cancellationToken))
            return Forbid();
        Normalize(model);
        await ValidateAsync(model, id, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.EstabelecimentoId = id;
            return View(model);
        }
        var entity = await db.Estabelecimentos.FirstOrDefaultAsync(x => x.EstabelecimentoId == id, cancellationToken);
        if (entity is null) return NotFound();
        ToEntity(model, entity);
        entity.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Estabelecimento atualizado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAsync(EstablishmentFormViewModel model, long? currentId, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(model.Codigo) && await db.Estabelecimentos.AnyAsync(
                x => x.Codigo == model.Codigo && x.EstabelecimentoId != currentId, ct))
            ModelState.AddModelError(nameof(model.Codigo), "Código já utilizado.");
        if (!string.IsNullOrWhiteSpace(model.Documento) && model.Documento!.Length is not (11 or 14))
            ModelState.AddModelError(nameof(model.Documento), "CPF/CNPJ deve possuir 11 ou 14 dígitos.");
        if (!string.IsNullOrWhiteSpace(model.Documento) && await db.Estabelecimentos.AnyAsync(
                x => x.Documento == model.Documento && x.EstabelecimentoId != currentId, ct))
            ModelState.AddModelError(nameof(model.Documento), "CPF/CNPJ já cadastrado.");
        if (!string.IsNullOrWhiteSpace(model.CEP) && model.CEP!.Length != 8)
            ModelState.AddModelError(nameof(model.CEP), "CEP deve possuir 8 dígitos.");
    }

    private static EstabelecimentoEntity ToEntity(EstablishmentFormViewModel m, EstabelecimentoEntity e)
    {
        e.Codigo = Clean(m.Codigo);
        e.NomeFantasia = m.NomeFantasia.Trim();
        e.RazaoSocial = Clean(m.RazaoSocial);
        e.Documento = Digits(m.Documento);
        e.InscricaoEstadual = Clean(m.InscricaoEstadual);
        e.Telefone = Clean(m.Telefone);
        e.Email = Clean(m.Email);
        e.Logradouro = Clean(m.Logradouro);
        e.Numero = Clean(m.Numero);
        e.Complemento = Clean(m.Complemento);
        e.Bairro = Clean(m.Bairro);
        e.Cidade = Clean(m.Cidade);
        e.UF = Clean(m.UF)?.ToUpperInvariant();
        e.CEP = Digits(m.CEP);
        e.HoraAbertura = m.HoraAbertura;
        e.HoraFechamento = m.HoraFechamento;
        e.Ativo = m.Ativo;
        return e;
    }

    private static void Normalize(EstablishmentFormViewModel m)
    {
        m.Documento = Digits(m.Documento);
        m.CEP = Digits(m.CEP);
        m.UF = Clean(m.UF)?.ToUpperInvariant();
    }
    private static string? Digits(string? value) => string.IsNullOrWhiteSpace(value) ? null : Regex.Replace(value, "[^0-9]", string.Empty);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
