using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Models;
using Naxapa.Web.Security;
using Naxapa.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Naxapa.Web.Controllers;

public sealed class HomeController(
    NaxapaDbContext db,
    CurrentUserService currentUser,
    NaxapaClock clock,
    DatabaseRuntimeState databaseState) : Controller
{
    public async Task<IActionResult> Index(
        string periodo = "hoje",
        DateTime? dataInicial = null,
        DateTime? dataFinal = null,
        CancellationToken cancellationToken = default)
    {
        if (!databaseState.IsReady)
            return RedirectToAction("Index", "Diagnostico");

        var estabelecimentoId = currentUser.EstabelecimentoId;
        var estabelecimento = await db.Estabelecimentos.AsNoTracking()
            .Where(x => x.EstabelecimentoId == estabelecimentoId)
            .Select(x => x.NomeFantasia)
            .SingleAsync(cancellationToken);

        var range = clock.GetUtcRange(periodo, dataInicial, dataFinal);
        var ordersInRange = db.Pedidos.AsNoTracking()
            .Where(x => x.EstabelecimentoId == estabelecimentoId
                        && x.CriadoEm >= range.StartUtc && x.CriadoEm < range.EndUtc);

        var orderCount = await ordersInRange.LongCountAsync(cancellationToken);
        var pendingCount = await ordersInRange.LongCountAsync(x => x.StatusOperacional == "PENDENTE", cancellationToken);
        var preparationCount = await ordersInRange.LongCountAsync(x => x.StatusOperacional == "EM_PREPARACAO", cancellationToken);
        var readyCount = await ordersInRange.LongCountAsync(x => x.StatusOperacional == "PRONTO", cancellationToken);
        var completedCount = await ordersInRange.LongCountAsync(x => x.StatusOperacional == "FINALIZADO", cancellationToken);
        var canceledCount = await ordersInRange.LongCountAsync(x => x.StatusOperacional == "CANCELADO", cancellationToken);

        var completed = db.Pedidos.AsNoTracking()
            .Where(x => x.EstabelecimentoId == estabelecimentoId
                        && x.StatusOperacional == "FINALIZADO"
                        && x.FinalizadoEm != null
                        && x.FinalizadoEm >= range.StartUtc && x.FinalizadoEm < range.EndUtc);
        var revenue = await completed.SumAsync(x => (decimal?)x.Total, cancellationToken) ?? 0m;
        var ticket = completedCount == 0 ? 0m : revenue / completedCount;

        var recentRaw = await (
            from p in db.Pedidos.AsNoTracking()
            join c0 in db.Clientes.AsNoTracking() on p.ClienteId equals c0.ClienteId into clients
            from c in clients.DefaultIfEmpty()
            where p.EstabelecimentoId == estabelecimentoId
            orderby p.CriadoEm descending
            select new
            {
                p.Numero,
                Cliente = c == null ? "Balcão" : c.Nome,
                p.TipoAtendimento,
                p.StatusOperacional,
                p.StatusFinanceiro,
                p.Total,
                p.CriadoEm
            })
            .Take(8)
            .ToListAsync(cancellationToken);
        var recentOrders = recentRaw.Select(x => new OrderRow(
            x.Numero, x.Cliente, x.TipoAtendimento, x.StatusOperacional, x.StatusFinanceiro, x.Total, clock.ToLocal(x.CriadoEm))).ToList();

        var topProductSales = await (
            from i in db.PedidoItens.AsNoTracking()
            join p in db.Pedidos.AsNoTracking() on i.PedidoId equals p.PedidoId
            where p.EstabelecimentoId == estabelecimentoId
                  && p.StatusOperacional == "FINALIZADO"
                  && p.FinalizadoEm != null
                  && p.FinalizadoEm >= range.StartUtc && p.FinalizadoEm < range.EndUtc
            group i by i.ProdutoId into g
            orderby g.Sum(x => x.Quantidade) descending
            select new { ProdutoId = g.Key, Quantidade = g.Sum(x => x.Quantidade) })
            .Take(5)
            .ToListAsync(cancellationToken);

        var productIds = topProductSales.Select(x => x.ProdutoId).ToList();
        var topProductsRaw = await (
            from p in db.Produtos.AsNoTracking()
            join c in db.Categorias.AsNoTracking() on p.CategoriaId equals c.CategoriaId
            join s0 in db.SetoresProducao.AsNoTracking() on p.SetorProducaoId equals s0.SetorProducaoId into sectors
            from s in sectors.DefaultIfEmpty()
            where p.EstabelecimentoId == estabelecimentoId && productIds.Contains(p.ProdutoId)
            select new ProductRow(p.ProdutoId, p.CodigoInterno ?? "-", p.Nome, c.Nome, p.PrecoVenda,
                s == null ? "Sem setor" : s.Nome, p.DisponivelCardapio, p.Ativo))
            .ToListAsync(cancellationToken);
        var rank = topProductSales.Select((x, i) => new { x.ProdutoId, i }).ToDictionary(x => x.ProdutoId, x => x.i);
        var topProducts = topProductsRaw.OrderBy(x => rank.TryGetValue(x.Id, out var i) ? i : int.MaxValue).ToList();

        var paymentMethods = await (
            from pg in db.Pagamentos.AsNoTracking()
            join fp in db.FormasPagamento.AsNoTracking() on pg.FormaPagamentoId equals fp.FormaPagamentoId
            where pg.EstabelecimentoId == estabelecimentoId
                  && pg.Status == "CONFIRMADO"
                  && pg.CriadoEm >= range.StartUtc && pg.CriadoEm < range.EndUtc
            group pg by fp.Nome into g
            orderby g.Sum(x => x.Valor) descending
            select new PaymentMethodMetric(g.Key, g.Sum(x => x.Valor), g.LongCount()))
            .ToListAsync(cancellationToken);

        var operators = await (
            from p in completed
            join u in db.Usuarios.AsNoTracking() on p.CriadoPorUsuarioId equals u.UsuarioId
            group p by u.Nome into g
            orderby g.Sum(x => x.Total) descending
            select new OperatorMetric(g.Key, g.Sum(x => x.Total), g.LongCount()))
            .Take(8)
            .ToListAsync(cancellationToken);

        var categories = await (
            from i in db.PedidoItens.AsNoTracking()
            join o in completed on i.PedidoId equals o.PedidoId
            join prod in db.Produtos.AsNoTracking() on i.ProdutoId equals prod.ProdutoId
            join cat in db.Categorias.AsNoTracking() on prod.CategoriaId equals cat.CategoriaId
            group i by cat.Nome into g
            orderby g.Sum(x => x.TotalItem) descending
            select new CategorySalesMetric(g.Key, g.Sum(x => x.TotalItem), g.Sum(x => x.Quantidade)))
            .Take(8)
            .ToListAsync(cancellationToken);

        var chartRaw = await completed
            .Select(x => new { x.FinalizadoEm, x.Total })
            .ToListAsync(cancellationToken);
        var chart = chartRaw
            .Where(x => x.FinalizadoEm.HasValue)
            .GroupBy(x => clock.ToLocal(x.FinalizadoEm!.Value).Date)
            .OrderBy(x => x.Key)
            .Select(x => new ChartPoint(x.Key.ToString("dd/MM"), x.Sum(v => v.Total)))
            .ToList();

        return View(new DashboardViewModel
        {
            Estabelecimento = estabelecimento,
            Periodo = periodo,
            PeriodoLabel = range.Label,
            DataInicial = dataInicial ?? clock.LocalNow.Date,
            DataFinal = dataFinal ?? clock.LocalNow.Date,
            Metrics =
            [
                new("Faturamento", revenue.ToString("C"), range.Label, "💰"),
                new("Pedidos", orderCount.ToString(), $"{pendingCount} pendente(s)", "🧾"),
                new("Em produção", preparationCount.ToString(), $"{readyCount} pronto(s)", "🍳"),
                new("Ticket médio", ticket.ToString("C"), $"{completedCount} finalizado(s) · {canceledCount} cancelado(s)", "📈")
            ],
            RecentOrders = recentOrders,
            TopProducts = topProducts,
            RevenueChart = chart,
            PaymentMethods = paymentMethods,
            Operators = operators,
            Categories = categories
        });
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}
