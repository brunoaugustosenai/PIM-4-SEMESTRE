using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Entities;
using Naxapa.Web.Models;
using Naxapa.Web.Security;
using Naxapa.Web.Services;

namespace Naxapa.Web.Controllers.Api;

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("api")]
public sealed class AuthApiController(NaxapaDbContext db, UserAuthenticationService authentication) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("token")]
    public async Task<ActionResult<ApiTokenResponse>> Token(ApiTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await authentication.AuthenticateAsync(request.Login, request.Password, request.EstabelecimentoId, cancellationToken);
        if (!result.Success || result.User is null || result.EstabelecimentoId is null)
            return Unauthorized(new ProblemDetails { Title = "Falha de autenticação", Detail = result.Error });

        var plain = SecurityHelpers.CreateOpaqueToken();
        var expires = DateTime.UtcNow.AddHours(12);
        db.RefreshTokens.Add(new RefreshTokenEntity
        {
            UsuarioId = result.User.UsuarioId,
            EstabelecimentoId = result.EstabelecimentoId.Value,
            TokenHash = SecurityHelpers.HashToken(plain),
            CriadoEm = DateTime.UtcNow,
            ExpiraEm = expires,
            EnderecoIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Truncate(Request.Headers["User-Agent"].ToString(), 300)
        });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new ApiTokenResponse(plain, "Bearer", expires, result.User.UsuarioId, result.User.Nome,
            result.EstabelecimentoId.Value, result.Estabelecimento ?? string.Empty));
    }

    private static string? Truncate(string? value, int maxLength)
        => string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(maxLength, value.Length)];

    [Authorize(AuthenticationSchemes = NaxapaAuthenticationSchemes.ApiBearer)]
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(CancellationToken cancellationToken)
    {
        var header = Request.Headers.Authorization.ToString();
        var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..].Trim() : string.Empty;
        if (token.Length < 32) return BadRequest();
        var hash = SecurityHelpers.HashToken(token);
        var row = await db.RefreshTokens.FirstOrDefaultAsync(x => x.RevogadoEm == null && x.TokenHash.SequenceEqual(hash), cancellationToken);
        if (row is not null) { row.RevogadoEm = DateTime.UtcNow; await db.SaveChangesAsync(cancellationToken); }
        return NoContent();
    }
}
