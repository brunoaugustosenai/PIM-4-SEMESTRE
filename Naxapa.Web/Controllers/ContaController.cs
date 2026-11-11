using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Entities;
using Naxapa.Web.Models;
using Naxapa.Web.Security;
using Naxapa.Web.Services;

namespace Naxapa.Web.Controllers;

public sealed class ContaController(
    NaxapaDbContext db,
    UserAuthenticationService authenticationService,
    CurrentUserService currentUser,
    IPasswordHasher<UsuarioEntity> passwordHasher) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (!await db.Usuarios.AsNoTracking().AnyAsync(cancellationToken))
            return RedirectToAction("Index", "Setup");

        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await authenticationService.AuthenticateAsync(model.Login, model.Password, null, cancellationToken);
        if (!result.Success || result.Principal is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Não foi possível autenticar.");
            return View(model);
        }

        await HttpContext.SignInAsync(NaxapaAuthenticationSchemes.Cookie, result.Principal, new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(model.RememberMe ? 24 : 8)
        });

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(NaxapaAuthenticationSchemes.Cookie);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AcessoNegado() => View();

    [Authorize]
    public async Task<IActionResult> Perfil(CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var establishmentId = currentUser.EstabelecimentoId;
        var user = await db.Usuarios.AsNoTracking().SingleAsync(x => x.UsuarioId == userId, cancellationToken);
        var establishment = await db.Estabelecimentos.AsNoTracking()
            .Where(x => x.EstabelecimentoId == establishmentId)
            .Select(x => x.NomeFantasia)
            .SingleAsync(cancellationToken);

        var establishments = await currentUser.GetAuthorizedEstablishmentsAsync(cancellationToken);
        return View(new ProfileViewModel
        {
            UsuarioId = user.UsuarioId,
            Nome = user.Nome,
            UserName = user.UserName,
            Email = user.Email,
            Telefone = user.Telefone,
            Perfil = currentUser.Perfil,
            Estabelecimento = establishment,
            Estabelecimentos = establishments.Select(x => new SelectListItem(x.Nome, x.Id.ToString(), x.Id == establishmentId)).ToList()
        });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> EditarPerfil(CancellationToken cancellationToken)
    {
        var user = await db.Usuarios.AsNoTracking().SingleAsync(x => x.UsuarioId == currentUser.UserId, cancellationToken);
        return View(new UserFormViewModel
        {
            UsuarioId = user.UsuarioId,
            Nome = user.Nome,
            UserName = user.UserName,
            Email = user.Email,
            Telefone = user.Telefone,
            Ativo = user.Ativo
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarPerfil(UserFormViewModel model, CancellationToken cancellationToken)
    {
        ModelState.Remove(nameof(model.PerfilId));
        ModelState.Remove(nameof(model.Password));
        if (!ModelState.IsValid)
            return View(model);

        var userId = currentUser.UserId;
        var normalizedUser = SecurityHelpers.NormalizeIdentity(model.UserName);
        var normalizedEmail = SecurityHelpers.NormalizeIdentity(model.Email);

        if (await db.Usuarios.AnyAsync(x => x.UsuarioId != userId && x.NormalizedUserName == normalizedUser, cancellationToken))
            ModelState.AddModelError(nameof(model.UserName), "Este usuário já está em uso.");
        if (await db.Usuarios.AnyAsync(x => x.UsuarioId != userId && x.NormalizedEmail == normalizedEmail, cancellationToken))
            ModelState.AddModelError(nameof(model.Email), "Este e-mail já está em uso.");
        if (!ModelState.IsValid)
            return View(model);

        var user = await db.Usuarios.SingleAsync(x => x.UsuarioId == userId, cancellationToken);
        user.Nome = model.Nome.Trim();
        user.UserName = model.UserName.Trim();
        user.NormalizedUserName = normalizedUser;
        user.Email = model.Email.Trim();
        user.NormalizedEmail = normalizedEmail;
        user.Telefone = Clean(model.Telefone);
        user.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var principal = await authenticationService.CreatePrincipalForUserAsync(userId, currentUser.EstabelecimentoId, cancellationToken);
        await HttpContext.SignInAsync(NaxapaAuthenticationSchemes.Cookie, principal);
        TempData["Success"] = "Perfil atualizado.";
        return RedirectToAction(nameof(Perfil));
    }

    [Authorize]
    [HttpGet]
    public IActionResult AlterarSenha() => View(new ChangePasswordViewModel());

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarSenha(ChangePasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await db.Usuarios.SingleAsync(x => x.UsuarioId == currentUser.UserId, cancellationToken);
        var verification = passwordHasher.VerifyHashedPassword(user, user.SenhaHash, model.CurrentPassword);
        if (verification == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Senha atual incorreta.");
            return View(model);
        }

        user.SenhaHash = passwordHasher.HashPassword(user, model.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.AtualizadoEm = DateTime.UtcNow;

        // Revoga tokens de API existentes após troca de senha.
        var tokens = await db.RefreshTokens.Where(x => x.UsuarioId == user.UsuarioId && x.RevogadoEm == null).ToListAsync(cancellationToken);
        foreach (var token in tokens)
            token.RevogadoEm = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Senha alterada com sucesso. Tokens de API anteriores foram revogados.";
        return RedirectToAction(nameof(Perfil));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TrocarEstabelecimento(long estabelecimentoId, CancellationToken cancellationToken)
    {
        if (!await currentUser.CanAccessEstablishmentAsync(estabelecimentoId, cancellationToken))
            return Forbid();

        var principal = await authenticationService.CreatePrincipalForUserAsync(currentUser.UserId, estabelecimentoId, cancellationToken);
        await HttpContext.SignInAsync(NaxapaAuthenticationSchemes.Cookie, principal);
        TempData["Success"] = "Estabelecimento ativo alterado.";
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public IActionResult Preferencias() => View();

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
