using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Security;
using Naxapa.Web.Services;

namespace Naxapa.Web.Controllers.Api;

[ApiController]
[Route("api/v1/admin")]
[Authorize(AuthenticationSchemes = NaxapaAuthenticationSchemes.ApiBearer)]
[EnableRateLimiting("api")]
public sealed class AdminApiController(NaxapaDbContext db, CurrentUserService currentUser, NaxapaClock clock) : ControllerBase
{
    [HttpGet("establishments")]
    [NaxapaPermission("ESTABELECIMENTO.VISUALIZAR")]
    public async Task<IActionResult> Establishments(CancellationToken ct)
    {
        var allowed = await currentUser.GetAuthorizedEstablishmentsAsync(ct);
        return Ok(allowed.Select(x => new { id = x.Id, name = x.Nome }));
    }

    [HttpGet("users")]
    [NaxapaPermission("USUARIO.VISUALIZAR")]
    public async Task<IActionResult> Users(CancellationToken ct)
    {
        var est = currentUser.EstabelecimentoId;
        var rows = await (
            from ue in db.UsuarioEstabelecimentos.AsNoTracking()
            join u in db.Usuarios.AsNoTracking() on ue.UsuarioId equals u.UsuarioId
            where ue.EstabelecimentoId == est && ue.Ativo
            orderby u.Nome
            select new { id = u.UsuarioId, u.Nome, username = u.UserName, u.Email, u.Ativo, u.UltimoAcessoEm })
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("profiles")]
    [NaxapaPermission("USUARIO.VISUALIZAR")]
    public async Task<IActionResult> Profiles(CancellationToken ct)
        => Ok(await db.Perfis.AsNoTracking().Where(x => x.Ativo).OrderBy(x => x.Nome)
            .Select(x => new { id = x.PerfilId, x.Nome, x.Escopo, x.Descricao }).ToListAsync(ct));

    [HttpGet("sectors")]
    [NaxapaPermission("SETOR.VISUALIZAR")]
    public async Task<IActionResult> Sectors(CancellationToken ct)
        => Ok(await db.SetoresProducao.AsNoTracking().Where(x => x.EstabelecimentoId == currentUser.EstabelecimentoId && x.Ativo)
            .OrderBy(x => x.Nome).Select(x => new { id = x.SetorProducaoId, x.Nome, x.Descricao }).ToListAsync(ct));

    [HttpGet("ingredients")]
    [NaxapaPermission("INSUMO.VISUALIZAR")]
    public async Task<IActionResult> Ingredients(CancellationToken ct)
        => Ok(await db.Insumos.AsNoTracking().Where(x => x.EstabelecimentoId == currentUser.EstabelecimentoId && x.Ativo)
            .OrderBy(x => x.Nome).Select(x => new { id = x.InsumoId, code = x.CodigoInterno, x.Nome, x.UnidadeMedida, cost = x.CustoMedio }).ToListAsync(ct));

    [HttpGet("recipes")]
    [NaxapaPermission("RECEITA.VISUALIZAR")]
    public async Task<IActionResult> Recipes(CancellationToken ct)
        => Ok(await (from r in db.Receitas.AsNoTracking()
                     join p in db.Produtos.AsNoTracking() on r.ProdutoId equals p.ProdutoId
                     where r.EstabelecimentoId == currentUser.EstabelecimentoId && r.Ativo
                     orderby p.Nome
                     select new { id = r.ReceitaId, productId = r.ProdutoId, product = p.Nome, r.Rendimento, r.Observacao }).ToListAsync(ct));

    [HttpGet("dashboard")]
    [NaxapaPermission("RELATORIO.VISUALIZAR")]
    public async Task<IActionResult> Dashboard(string period = "hoje", DateTime? start = null, DateTime? end = null, CancellationToken ct = default)
    {
        var range = clock.GetUtcRange(period, start, end);
        var est = currentUser.EstabelecimentoId;
        var q = db.Pedidos.AsNoTracking().Where(x => x.EstabelecimentoId == est && x.CriadoEm >= range.StartUtc && x.CriadoEm < range.EndUtc);
        var totalOrders = await q.LongCountAsync(ct);
        var finalized = q.Where(x => x.StatusOperacional == "FINALIZADO");
        var revenue = await finalized.SumAsync(x => (decimal?)x.Total, ct) ?? 0;
        var finalizedCount = await finalized.LongCountAsync(ct);
        var byStatus = await q.GroupBy(x => x.StatusOperacional).Select(g => new { status = g.Key, count = g.LongCount() }).ToListAsync(ct);
        var payments = await (from p in db.Pagamentos.AsNoTracking()
                              join o in db.Pedidos.AsNoTracking() on p.PedidoId equals o.PedidoId
                              join f in db.FormasPagamento.AsNoTracking() on p.FormaPagamentoId equals f.FormaPagamentoId
                              where p.EstabelecimentoId == est && p.Status == "CONFIRMADO" && o.CriadoEm >= range.StartUtc && o.CriadoEm < range.EndUtc
                              group p by f.Nome into g
                              select new { method = g.Key, value = g.Sum(x => x.Valor) }).ToListAsync(ct);
        return Ok(new { range.StartUtc, range.EndUtc, totalOrders, finalizedCount, revenue, averageTicket = finalizedCount == 0 ? 0 : revenue / finalizedCount, byStatus, payments });
    }
}
