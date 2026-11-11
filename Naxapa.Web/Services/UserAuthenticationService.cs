using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Entities;
using Naxapa.Web.Security;

namespace Naxapa.Web.Services;

public sealed record AuthenticationResult(
    bool Success,
    string? Error,
    UsuarioEntity? User,
    long? EstabelecimentoId,
    string? Estabelecimento,
    string? Perfil,
    ClaimsPrincipal? Principal)
{
    public static AuthenticationResult Fail(string error) => new(false, error, null, null, null, null, null);
}

public sealed class UserAuthenticationService(
    NaxapaDbContext db,
    IPasswordHasher<UsuarioEntity> passwordHasher)
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<AuthenticationResult> AuthenticateAsync(
        string login,
        string password,
        long? requestedEstablishmentId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = SecurityHelpers.NormalizeIdentity(login);
        var user = await db.Usuarios.FirstOrDefaultAsync(
            x => x.NormalizedUserName == normalized || x.NormalizedEmail == normalized,
            cancellationToken);

        // Mensagem genérica para evitar enumeração de usuários.
        if (user is null || !user.Ativo)
            return AuthenticationResult.Fail("Usuário ou senha inválidos.");

        var now = DateTimeOffset.UtcNow;
        if (user.BloqueioHabilitado && user.BloqueioFim is not null && user.BloqueioFim > now)
            return AuthenticationResult.Fail("Acesso temporariamente bloqueado após tentativas inválidas. Tente novamente mais tarde.");

        var verification = passwordHasher.VerifyHashedPassword(user, user.SenhaHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            user.TentativasAcessoFalho++;
            if (user.BloqueioHabilitado && user.TentativasAcessoFalho >= MaxFailedAttempts)
            {
                user.BloqueioFim = now.Add(LockoutDuration);
                user.TentativasAcessoFalho = 0;
            }

            await db.SaveChangesAsync(cancellationToken);
            return AuthenticationResult.Fail("Usuário ou senha inválidos.");
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            user.SenhaHash = passwordHasher.HashPassword(user, password);

        var globalAdmin = await (
            from ug in db.UsuarioPerfisGlobais.AsNoTracking()
            join p in db.Perfis.AsNoTracking() on ug.PerfilId equals p.PerfilId
            where ug.UsuarioId == user.UsuarioId && p.Ativo && p.Nome == "Administrador Geral"
            select p.PerfilId)
            .AnyAsync(cancellationToken);

        long? estabelecimentoId = null;
        if (requestedEstablishmentId is not null)
        {
            var allowed = globalAdmin
                ? await db.Estabelecimentos.AsNoTracking().AnyAsync(
                    x => x.EstabelecimentoId == requestedEstablishmentId.Value && x.Ativo,
                    cancellationToken)
                : await db.UsuarioEstabelecimentos.AsNoTracking().AnyAsync(
                    x => x.UsuarioId == user.UsuarioId
                         && x.EstabelecimentoId == requestedEstablishmentId.Value
                         && x.Ativo,
                    cancellationToken);

            if (allowed)
                estabelecimentoId = requestedEstablishmentId.Value;
        }

        if (estabelecimentoId is null && !globalAdmin)
        {
            estabelecimentoId = await db.UsuarioEstabelecimentos.AsNoTracking()
                .Where(x => x.UsuarioId == user.UsuarioId && x.Ativo)
                .OrderByDescending(x => x.Principal)
                .ThenBy(x => x.UsuarioEstabelecimentoId)
                .Select(x => (long?)x.EstabelecimentoId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (estabelecimentoId is null && globalAdmin)
        {
            estabelecimentoId = await db.Estabelecimentos.AsNoTracking()
                .Where(x => x.Ativo)
                .OrderBy(x => x.EstabelecimentoId)
                .Select(x => (long?)x.EstabelecimentoId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (estabelecimentoId is null)
            return AuthenticationResult.Fail("O usuário não possui estabelecimento autorizado.");

        var estabelecimento = await db.Estabelecimentos.AsNoTracking()
            .Where(x => x.EstabelecimentoId == estabelecimentoId.Value && x.Ativo)
            .Select(x => x.NomeFantasia)
            .FirstOrDefaultAsync(cancellationToken);

        if (estabelecimento is null)
            return AuthenticationResult.Fail("Estabelecimento inativo ou inexistente.");

        var profileNames = await GetProfileNamesAsync(user.UsuarioId, estabelecimentoId.Value, cancellationToken);
        var principal = CreatePrincipal(user, estabelecimentoId.Value, profileNames);

        user.TentativasAcessoFalho = 0;
        user.BloqueioFim = null;
        user.UltimoAcessoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new AuthenticationResult(
            true,
            null,
            user,
            estabelecimentoId,
            estabelecimento,
            profileNames.FirstOrDefault() ?? "Usuário",
            principal);
    }

    public async Task<ClaimsPrincipal> CreatePrincipalForUserAsync(
        long userId,
        long estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UsuarioId == userId && x.Ativo, cancellationToken)
            ?? throw new InvalidOperationException("Usuário não encontrado ou inativo.");

        var profileNames = await GetProfileNamesAsync(userId, estabelecimentoId, cancellationToken);
        return CreatePrincipal(user, estabelecimentoId, profileNames);
    }

    private async Task<List<string>> GetProfileNamesAsync(long userId, long estabelecimentoId, CancellationToken cancellationToken)
    {
        var global = await (
            from ug in db.UsuarioPerfisGlobais.AsNoTracking()
            join p in db.Perfis.AsNoTracking() on ug.PerfilId equals p.PerfilId
            where ug.UsuarioId == userId && p.Ativo
            select p.Nome)
            .ToListAsync(cancellationToken);

        var local = await (
            from ue in db.UsuarioEstabelecimentos.AsNoTracking()
            join uep in db.UsuarioEstabelecimentoPerfis.AsNoTracking()
                on ue.UsuarioEstabelecimentoId equals uep.UsuarioEstabelecimentoId
            join p in db.Perfis.AsNoTracking() on uep.PerfilId equals p.PerfilId
            where ue.UsuarioId == userId
                  && ue.EstabelecimentoId == estabelecimentoId
                  && ue.Ativo
                  && p.Ativo
            select p.Nome)
            .ToListAsync(cancellationToken);

        return global.Concat(local).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static ClaimsPrincipal CreatePrincipal(UsuarioEntity user, long estabelecimentoId, IReadOnlyCollection<string> profiles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UsuarioId.ToString(CultureInfo.InvariantCulture)),
            new(NaxapaClaimTypes.UserId, user.UsuarioId.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, user.Nome),
            new(ClaimTypes.GivenName, user.Nome),
            new(ClaimTypes.Email, user.Email),
            new(NaxapaClaimTypes.EstabelecimentoId, estabelecimentoId.ToString(CultureInfo.InvariantCulture)),
            new(NaxapaClaimTypes.Perfil, profiles.FirstOrDefault() ?? "Usuário")
        };

        foreach (var profile in profiles)
            claims.Add(new Claim(ClaimTypes.Role, profile));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, NaxapaAuthenticationSchemes.Cookie));
    }
}
