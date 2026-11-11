using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Entities;
using Naxapa.Web.Models;
using Naxapa.Web.Security;

namespace Naxapa.Web.Controllers.Api;

[ApiController]
[Route("api/v1")]
[Authorize(AuthenticationSchemes = NaxapaAuthenticationSchemes.ApiBearer)]
[EnableRateLimiting("api")]
public sealed class OperationsApiController(NaxapaDbContext db, CurrentUserService currentUser) : ControllerBase
{
    [HttpGet("tables")]
    [NaxapaPermission("PEDIDO.VISUALIZAR")]
    public async Task<ActionResult<IReadOnlyList<ApiTableDto>>> Tables(CancellationToken cancellationToken)
        => Ok(await db.Mesas.AsNoTracking()
            .Where(x => x.EstabelecimentoId == currentUser.EstabelecimentoId && x.Ativo)
            .OrderBy(x => x.Numero)
            .Select(x => new ApiTableDto(x.MesaId, x.Numero, x.Nome, x.Capacidade, x.Status, x.Ativo))
            .ToListAsync(cancellationToken));

    [HttpPost("tables")]
    [NaxapaPermission("PEDIDO.EDITAR")]
    public async Task<ActionResult<ApiTableDto>> CreateTable(ApiTableCreateRequest request, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        if (await db.Mesas.AnyAsync(x => x.EstabelecimentoId == est && x.Numero == request.Number, cancellationToken))
            return Conflict(new ProblemDetails { Title = "Já existe uma mesa com esse número." });
        var entity = new MesaEntity { EstabelecimentoId = est, Numero = request.Number, Nome = Clean(request.Name), Capacidade = request.Capacity, Status = "LIVRE", Ativo = true };
        db.Mesas.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Created($"/api/v1/tables/{entity.MesaId}", new ApiTableDto(entity.MesaId, entity.Numero, entity.Nome, entity.Capacidade, entity.Status, entity.Ativo));
    }

    [HttpGet("tabs")]
    [NaxapaPermission("PEDIDO.VISUALIZAR")]
    public async Task<ActionResult<IReadOnlyList<ApiTabDto>>> Tabs(string? status = null, CancellationToken cancellationToken = default)
    {
        var query = db.Comandas.AsNoTracking().Where(x => x.EstabelecimentoId == currentUser.EstabelecimentoId);
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim().ToUpperInvariant();
            query = query.Where(x => x.Status == normalized);
        }
        return Ok(await query.OrderBy(x => x.Codigo)
            .Select(x => new ApiTabDto(x.ComandaId, x.Codigo, x.Status, x.ClienteId, x.AbertaEm, x.FechadaEm, x.Observacao))
            .ToListAsync(cancellationToken));
    }

    [HttpPost("tabs/open")]
    [NaxapaPermission("PEDIDO.CRIAR")]
    public async Task<ActionResult<ApiTabDto>> OpenTab(ApiTabCreateRequest request, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var code = request.Code.Trim();
        var entity = await db.Comandas.FirstOrDefaultAsync(x => x.EstabelecimentoId == est && x.Codigo == code, cancellationToken);
        if (entity is null)
        {
            entity = new ComandaEntity { EstabelecimentoId = est, Codigo = code, Status = "ABERTA", ClienteId = request.ClientId, AbertaEm = DateTime.UtcNow, Observacao = Clean(request.Notes) };
            db.Comandas.Add(entity);
        }
        else
        {
            if (entity.Status == "ABERTA") return Conflict(new ProblemDetails { Title = "A comanda já está aberta." });
            entity.Status = "ABERTA"; entity.ClienteId = request.ClientId; entity.AbertaEm = DateTime.UtcNow; entity.FechadaEm = null; entity.Observacao = Clean(request.Notes);
        }
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new ApiTabDto(entity.ComandaId, entity.Codigo, entity.Status, entity.ClienteId, entity.AbertaEm, entity.FechadaEm, entity.Observacao));
    }

    [HttpPost("tabs/{id:long}/close")]
    [NaxapaPermission("PEDIDO.EDITAR")]
    public async Task<IActionResult> CloseTab(long id, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var entity = await db.Comandas.FirstOrDefaultAsync(x => x.ComandaId == id && x.EstabelecimentoId == est, cancellationToken);
        if (entity is null) return NotFound();
        if (entity.Status != "ABERTA") return Conflict(new ProblemDetails { Title = "A comanda não está aberta." });
        var hasOpenOrder = await db.Pedidos.AsNoTracking().AnyAsync(x => x.EstabelecimentoId == est && x.ComandaId == id && x.StatusOperacional != "FINALIZADO" && x.StatusOperacional != "CANCELADO", cancellationToken);
        if (hasOpenOrder) return Conflict(new ProblemDetails { Title = "Existem pedidos em aberto vinculados à comanda." });
        entity.Status = "FECHADA"; entity.FechadaEm = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
