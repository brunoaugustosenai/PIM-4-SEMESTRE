using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Entities;
using Naxapa.Web.Models;
using Naxapa.Web.Security;

namespace Naxapa.Web.Controllers;

[Authorize]
public sealed class UsuariosController(
    NaxapaDbContext db,
    CurrentUserService currentUser,
    IPasswordHasher<UsuarioEntity> passwordHasher) : Controller
{
    [NaxapaPermission("USUARIO.VISUALIZAR")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var rows = await (
            from ue in db.UsuarioEstabelecimentos.AsNoTracking()
            join u in db.Usuarios.AsNoTracking() on ue.UsuarioId equals u.UsuarioId
            where ue.EstabelecimentoId == est && ue.Ativo
            orderby u.Nome
            select new { ue.UsuarioEstabelecimentoId, u })
            .ToListAsync(cancellationToken);

        var linkIds = rows.Select(x => x.UsuarioEstabelecimentoId).ToArray();
        var roles = await (
            from uep in db.UsuarioEstabelecimentoPerfis.AsNoTracking()
            join p in db.Perfis.AsNoTracking() on uep.PerfilId equals p.PerfilId
            where linkIds.Contains(uep.UsuarioEstabelecimentoId)
            select new { uep.UsuarioEstabelecimentoId, p.Nome })
            .ToListAsync(cancellationToken);

        var result = rows.Select(x => new UserRow(
            x.u.UsuarioId,
            x.u.Nome,
            x.u.UserName,
            x.u.Email,
            string.Join(", ", roles.Where(r => r.UsuarioEstabelecimentoId == x.UsuarioEstabelecimentoId).Select(r => r.Nome)),
            x.u.Ativo,
            x.u.UltimoAcessoEm)).ToList();

        return View(result);
    }

    [NaxapaPermission("USUARIO.CRIAR")]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new UserFormViewModel();
        await LoadProfilesAsync(model, cancellationToken);
        return View(model);
    }

    [NaxapaPermission("USUARIO.CRIAR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
            ModelState.AddModelError(nameof(model.Password), "Informe uma senha inicial com pelo menos 10 caracteres.");

        var normalizedUser = SecurityHelpers.NormalizeIdentity(model.UserName);
        var normalizedEmail = SecurityHelpers.NormalizeIdentity(model.Email);
        if (await db.Usuarios.AnyAsync(x => x.NormalizedUserName == normalizedUser, cancellationToken))
            ModelState.AddModelError(nameof(model.UserName), "Este usuário já está cadastrado.");
        if (await db.Usuarios.AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken))
            ModelState.AddModelError(nameof(model.Email), "Este e-mail já está cadastrado.");

        var profile = await db.Perfis.AsNoTracking().FirstOrDefaultAsync(x => x.PerfilId == model.PerfilId && x.Ativo, cancellationToken);
        if (profile is null)
            ModelState.AddModelError(nameof(model.PerfilId), "Perfil inválido.");
        else if (profile.Escopo == "SISTEMA" && !await currentUser.IsGlobalAdminAsync(cancellationToken))
            ModelState.AddModelError(nameof(model.PerfilId), "Somente um Administrador Geral pode atribuir perfis de sistema.");

        if (!ModelState.IsValid)
        {
            await LoadProfilesAsync(model, cancellationToken);
            return View(model);
        }

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        var user = new UsuarioEntity
        {
            Nome = model.Nome.Trim(),
            UserName = model.UserName.Trim(),
            NormalizedUserName = normalizedUser,
            Email = model.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            EmailConfirmado = true,
            Telefone = Clean(model.Telefone),
            Ativo = model.Ativo,
            BloqueioHabilitado = true,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            CriadoEm = DateTime.UtcNow
        };
        user.SenhaHash = passwordHasher.HashPassword(user, model.Password!);
        db.Usuarios.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var link = new UsuarioEstabelecimentoEntity
        {
            UsuarioId = user.UsuarioId,
            EstabelecimentoId = currentUser.EstabelecimentoId,
            Principal = true,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };
        db.UsuarioEstabelecimentos.Add(link);
        await db.SaveChangesAsync(cancellationToken);

        if (profile!.Escopo == "SISTEMA")
        {
            db.UsuarioPerfisGlobais.Add(new UsuarioPerfilGlobalEntity { UsuarioId = user.UsuarioId, PerfilId = profile.PerfilId });
        }
        else
        {
            db.UsuarioEstabelecimentoPerfis.Add(new UsuarioEstabelecimentoPerfilEntity
            {
                UsuarioEstabelecimentoId = link.UsuarioEstabelecimentoId,
                PerfilId = profile.PerfilId
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        TempData["Success"] = "Usuário cadastrado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("USUARIO.EDITAR")]
    [HttpGet]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var link = await db.UsuarioEstabelecimentos.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UsuarioId == id && x.EstabelecimentoId == est && x.Ativo, cancellationToken);
        if (link is null)
            return NotFound();

        var user = await db.Usuarios.AsNoTracking().SingleAsync(x => x.UsuarioId == id, cancellationToken);
        var profileId = await db.UsuarioEstabelecimentoPerfis.AsNoTracking()
            .Where(x => x.UsuarioEstabelecimentoId == link.UsuarioEstabelecimentoId)
            .Select(x => (long?)x.PerfilId)
            .FirstOrDefaultAsync(cancellationToken);

        profileId ??= await db.UsuarioPerfisGlobais.AsNoTracking()
            .Where(x => x.UsuarioId == id)
            .Select(x => (long?)x.PerfilId)
            .FirstOrDefaultAsync(cancellationToken);

        var model = new UserFormViewModel
        {
            UsuarioId = user.UsuarioId,
            Nome = user.Nome,
            UserName = user.UserName,
            Email = user.Email,
            Telefone = user.Telefone,
            PerfilId = profileId ?? 0,
            Ativo = user.Ativo
        };
        await LoadProfilesAsync(model, cancellationToken);
        return View(model);
    }

    [NaxapaPermission("USUARIO.EDITAR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, UserFormViewModel model, CancellationToken cancellationToken)
    {
        ModelState.Remove(nameof(model.Password));
        var est = currentUser.EstabelecimentoId;
        var link = await db.UsuarioEstabelecimentos.FirstOrDefaultAsync(
            x => x.UsuarioId == id && x.EstabelecimentoId == est && x.Ativo,
            cancellationToken);
        if (link is null)
            return NotFound();

        if (id == currentUser.UserId && !model.Ativo)
            ModelState.AddModelError(nameof(model.Ativo), "Você não pode desativar o próprio usuário enquanto está conectado.");

        var normalizedUser = SecurityHelpers.NormalizeIdentity(model.UserName);
        var normalizedEmail = SecurityHelpers.NormalizeIdentity(model.Email);
        if (await db.Usuarios.AnyAsync(x => x.UsuarioId != id && x.NormalizedUserName == normalizedUser, cancellationToken))
            ModelState.AddModelError(nameof(model.UserName), "Este usuário já está cadastrado.");
        if (await db.Usuarios.AnyAsync(x => x.UsuarioId != id && x.NormalizedEmail == normalizedEmail, cancellationToken))
            ModelState.AddModelError(nameof(model.Email), "Este e-mail já está cadastrado.");

        var profile = await db.Perfis.AsNoTracking().FirstOrDefaultAsync(x => x.PerfilId == model.PerfilId && x.Ativo, cancellationToken);
        var actorIsGlobalAdmin = await currentUser.IsGlobalAdminAsync(cancellationToken);
        var targetHasGlobalProfile = await db.UsuarioPerfisGlobais.AsNoTracking().AnyAsync(x => x.UsuarioId == id, cancellationToken);
        if (profile is null)
            ModelState.AddModelError(nameof(model.PerfilId), "Perfil inválido.");
        else if (profile.Escopo == "SISTEMA" && !actorIsGlobalAdmin)
            ModelState.AddModelError(nameof(model.PerfilId), "Somente um Administrador Geral pode atribuir perfis de sistema.");
        if (targetHasGlobalProfile && !actorIsGlobalAdmin)
            ModelState.AddModelError(nameof(model.PerfilId), "Somente um Administrador Geral pode alterar um perfil de sistema.");

        if (!ModelState.IsValid)
        {
            model.UsuarioId = id;
            await LoadProfilesAsync(model, cancellationToken);
            return View(model);
        }

        var user = await db.Usuarios.SingleAsync(x => x.UsuarioId == id, cancellationToken);
        user.Nome = model.Nome.Trim();
        user.UserName = model.UserName.Trim();
        user.NormalizedUserName = normalizedUser;
        user.Email = model.Email.Trim();
        user.NormalizedEmail = normalizedEmail;
        user.Telefone = Clean(model.Telefone);
        user.Ativo = model.Ativo;
        user.AtualizadoEm = DateTime.UtcNow;

        var existingLocalRoles = await db.UsuarioEstabelecimentoPerfis
            .Where(x => x.UsuarioEstabelecimentoId == link.UsuarioEstabelecimentoId)
            .ToListAsync(cancellationToken);
        db.UsuarioEstabelecimentoPerfis.RemoveRange(existingLocalRoles);

        var existingGlobalRoles = await db.UsuarioPerfisGlobais
            .Where(x => x.UsuarioId == id)
            .ToListAsync(cancellationToken);

        if (profile!.Escopo == "SISTEMA")
        {
            if (actorIsGlobalAdmin)
                db.UsuarioPerfisGlobais.RemoveRange(existingGlobalRoles.Where(x => x.PerfilId != profile.PerfilId));
            if (!existingGlobalRoles.Any(x => x.PerfilId == profile.PerfilId))
                db.UsuarioPerfisGlobais.Add(new UsuarioPerfilGlobalEntity { UsuarioId = id, PerfilId = profile.PerfilId });
        }
        else
        {
            if (actorIsGlobalAdmin)
                db.UsuarioPerfisGlobais.RemoveRange(existingGlobalRoles);
            db.UsuarioEstabelecimentoPerfis.Add(new UsuarioEstabelecimentoPerfilEntity
            {
                UsuarioEstabelecimentoId = link.UsuarioEstabelecimentoId,
                PerfilId = profile.PerfilId
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Usuário atualizado.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("USUARIO.EDITAR")]
    [HttpGet]
    public async Task<IActionResult> ResetPassword(long id, CancellationToken cancellationToken)
    {
        var allowed = await db.UsuarioEstabelecimentos.AsNoTracking().AnyAsync(
            x => x.UsuarioId == id && x.EstabelecimentoId == currentUser.EstabelecimentoId && x.Ativo,
            cancellationToken);
        if (!allowed)
            return NotFound();

        var name = await db.Usuarios.AsNoTracking().Where(x => x.UsuarioId == id).Select(x => x.Nome).SingleAsync(cancellationToken);
        return View(new ResetPasswordViewModel { UsuarioId = id, Usuario = name });
    }

    [NaxapaPermission("USUARIO.EDITAR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken cancellationToken)
    {
        var allowed = await db.UsuarioEstabelecimentos.AsNoTracking().AnyAsync(
            x => x.UsuarioId == model.UsuarioId && x.EstabelecimentoId == currentUser.EstabelecimentoId && x.Ativo,
            cancellationToken);
        if (!allowed)
            return NotFound();
        if (!ModelState.IsValid)
            return View(model);

        var user = await db.Usuarios.SingleAsync(x => x.UsuarioId == model.UsuarioId, cancellationToken);
        user.SenhaHash = passwordHasher.HashPassword(user, model.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.TentativasAcessoFalho = 0;
        user.BloqueioFim = null;
        user.AtualizadoEm = DateTime.UtcNow;

        var tokens = await db.RefreshTokens.Where(x => x.UsuarioId == user.UsuarioId && x.RevogadoEm == null).ToListAsync(cancellationToken);
        foreach (var token in tokens)
            token.RevogadoEm = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Senha redefinida. Tokens de API anteriores foram revogados.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadProfilesAsync(UserFormViewModel model, CancellationToken cancellationToken)
    {
        var isGlobalAdmin = await currentUser.IsGlobalAdminAsync(cancellationToken);
        model.Perfis = await db.Perfis.AsNoTracking()
            .Where(x => x.Ativo && (x.Escopo == "ESTABELECIMENTO" || isGlobalAdmin))
            .OrderBy(x => x.Escopo).ThenBy(x => x.Nome)
            .Select(x => new SelectListItem(x.Nome, x.PerfilId.ToString()))
            .ToListAsync(cancellationToken);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
