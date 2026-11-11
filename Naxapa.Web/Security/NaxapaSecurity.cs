using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Naxapa.Web.Data;

namespace Naxapa.Web.Security;

public static class NaxapaAuthenticationSchemes
{
    public const string Cookie = CookieAuthenticationDefaults.AuthenticationScheme;
    public const string ApiBearer = "NaxapaApiBearer";
}

public static class NaxapaClaimTypes
{
    public const string UserId = "naxapa:user_id";
    public const string EstabelecimentoId = "naxapa:estabelecimento_id";
    public const string Perfil = "naxapa:perfil";
}

public sealed class NaxapaPermissionAttribute(string permission) : AuthorizeAttribute($"Permission:{permission}");

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        const string prefix = "Permission:";
        if (!policyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return _fallback.GetPolicyAsync(policyName);

        var permission = policyName[prefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(permission))
            return Task.FromResult<AuthorizationPolicy?>(null);

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}

public sealed class PermissionAuthorizationHandler(NaxapaDbContext db) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (!TryGetLongClaim(context.User, NaxapaClaimTypes.UserId, out var userId))
            return;

        var permissionKey = requirement.Permission.Trim();

        var globalAllowed = await (
            from ug in db.UsuarioPerfisGlobais.AsNoTracking()
            join p in db.Perfis.AsNoTracking() on ug.PerfilId equals p.PerfilId
            join pp in db.PerfilPermissoes.AsNoTracking() on p.PerfilId equals pp.PerfilId
            join perm in db.Permissoes.AsNoTracking() on pp.PermissaoId equals perm.PermissaoId
            where ug.UsuarioId == userId
                  && p.Ativo
                  && perm.Ativo
                  && perm.Chave == permissionKey
            select perm.PermissaoId)
            .AnyAsync();

        if (globalAllowed)
        {
            context.Succeed(requirement);
            return;
        }

        if (!TryGetLongClaim(context.User, NaxapaClaimTypes.EstabelecimentoId, out var estabelecimentoId))
            return;

        var establishmentAllowed = await (
            from ue in db.UsuarioEstabelecimentos.AsNoTracking()
            join uep in db.UsuarioEstabelecimentoPerfis.AsNoTracking()
                on ue.UsuarioEstabelecimentoId equals uep.UsuarioEstabelecimentoId
            join p in db.Perfis.AsNoTracking() on uep.PerfilId equals p.PerfilId
            join pp in db.PerfilPermissoes.AsNoTracking() on p.PerfilId equals pp.PerfilId
            join perm in db.Permissoes.AsNoTracking() on pp.PermissaoId equals perm.PermissaoId
            where ue.UsuarioId == userId
                  && ue.EstabelecimentoId == estabelecimentoId
                  && ue.Ativo
                  && p.Ativo
                  && perm.Ativo
                  && perm.Chave == permissionKey
            select perm.PermissaoId)
            .AnyAsync();

        if (establishmentAllowed)
            context.Succeed(requirement);
    }

    private static bool TryGetLongClaim(ClaimsPrincipal user, string claimType, out long value)
        => long.TryParse(user.FindFirstValue(claimType), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
}

public sealed class ApiTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IServiceScopeFactory scopeFactory)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = header["Bearer ".Length..].Trim();
        if (token.Length < 32)
            return AuthenticateResult.Fail("Token inválido.");

        var tokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(token));

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NaxapaDbContext>();
        var now = DateTime.UtcNow;

        var tokenRow = await db.RefreshTokens.AsNoTracking()
            .Where(x => x.RevogadoEm == null && x.ExpiraEm > now && x.TokenHash.SequenceEqual(tokenHash))
            .Select(x => new { x.UsuarioId, x.EstabelecimentoId, x.ExpiraEm })
            .FirstOrDefaultAsync(Context.RequestAborted);

        if (tokenRow is null || tokenRow.EstabelecimentoId is null)
            return AuthenticateResult.Fail("Token expirado ou revogado.");

        var user = await db.Usuarios.AsNoTracking()
            .Where(x => x.UsuarioId == tokenRow.UsuarioId && x.Ativo)
            .Select(x => new { x.UsuarioId, x.Nome, x.UserName })
            .FirstOrDefaultAsync(Context.RequestAborted);

        if (user is null)
            return AuthenticateResult.Fail("Usuário inativo ou inexistente.");

        var linked = await db.UsuarioEstabelecimentos.AsNoTracking()
            .AnyAsync(x => x.UsuarioId == user.UsuarioId
                           && x.EstabelecimentoId == tokenRow.EstabelecimentoId.Value
                           && x.Ativo,
                Context.RequestAborted);

        if (!linked)
            return AuthenticateResult.Fail("Usuário não autorizado para o estabelecimento do token.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UsuarioId.ToString(CultureInfo.InvariantCulture)),
            new(NaxapaClaimTypes.UserId, user.UsuarioId.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, user.Nome),
            new(ClaimTypes.GivenName, user.Nome),
            new(NaxapaClaimTypes.EstabelecimentoId, tokenRow.EstabelecimentoId.Value.ToString(CultureInfo.InvariantCulture))
        };

        var identity = new ClaimsIdentity(claims, NaxapaAuthenticationSchemes.ApiBearer);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}

public sealed class CurrentUserService(IHttpContextAccessor accessor, NaxapaDbContext db)
{
    private ClaimsPrincipal User => accessor.HttpContext?.User ?? new ClaimsPrincipal();

    public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;

    public long UserId => TryGetLong(NaxapaClaimTypes.UserId)
        ?? throw new InvalidOperationException("Usuário autenticado sem identificador.");

    public long EstabelecimentoId => TryGetLong(NaxapaClaimTypes.EstabelecimentoId)
        ?? throw new InvalidOperationException("Usuário autenticado sem estabelecimento ativo.");

    public string Nome => User.Identity?.Name ?? "Usuário";

    public string Perfil => User.FindFirstValue(NaxapaClaimTypes.Perfil) ?? "Usuário";

    public async Task<bool> CanAccessEstablishmentAsync(long estabelecimentoId, CancellationToken cancellationToken = default)
    {
        if (!IsAuthenticated)
            return false;

        var userId = UserId;

        if (await IsGlobalAdminAsync(cancellationToken))
            return await db.Estabelecimentos.AsNoTracking()
                .AnyAsync(x => x.EstabelecimentoId == estabelecimentoId && x.Ativo, cancellationToken);

        return await db.UsuarioEstabelecimentos.AsNoTracking()
            .AnyAsync(x => x.UsuarioId == userId
                           && x.EstabelecimentoId == estabelecimentoId
                           && x.Ativo,
                cancellationToken);
    }

    public async Task<bool> IsGlobalAdminAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAuthenticated)
            return false;

        var userId = UserId;
        return await (
            from ug in db.UsuarioPerfisGlobais.AsNoTracking()
            join p in db.Perfis.AsNoTracking() on ug.PerfilId equals p.PerfilId
            where ug.UsuarioId == userId && p.Ativo && p.Nome == "Administrador Geral"
            select p.PerfilId)
            .AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<(long Id, string Nome)>> GetAuthorizedEstablishmentsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAuthenticated)
            return [];

        if (await IsGlobalAdminAsync(cancellationToken))
        {
            var rows = await db.Estabelecimentos.AsNoTracking()
                .Where(x => x.Ativo)
                .OrderBy(x => x.NomeFantasia)
                .Select(x => new { x.EstabelecimentoId, x.NomeFantasia })
                .ToListAsync(cancellationToken);
            return rows.Select(x => (x.EstabelecimentoId, x.NomeFantasia)).ToList();
        }

        var userId = UserId;
        var linked = await (
            from ue in db.UsuarioEstabelecimentos.AsNoTracking()
            join e in db.Estabelecimentos.AsNoTracking() on ue.EstabelecimentoId equals e.EstabelecimentoId
            where ue.UsuarioId == userId && ue.Ativo && e.Ativo
            orderby e.NomeFantasia
            select new { e.EstabelecimentoId, e.NomeFantasia })
            .ToListAsync(cancellationToken);
        return linked.Select(x => (x.EstabelecimentoId, x.NomeFantasia)).ToList();
    }

    private long? TryGetLong(string claimType)
        => long.TryParse(User.FindFirstValue(claimType), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
}

public static class SecurityHelpers
{
    public static string NormalizeIdentity(string value) => value.Trim().ToUpperInvariant();

    public static string CreateOpaqueToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    public static byte[] HashToken(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));

    public static bool IsLoopback(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress;
        return ip is null || IPAddress.IsLoopback(ip);
    }
}
