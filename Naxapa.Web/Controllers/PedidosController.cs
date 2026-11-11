using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Models;
using Naxapa.Web.Security;
using Naxapa.Web.Services;

namespace Naxapa.Web.Controllers;

public sealed class PedidosController(NaxapaDbContext db, CurrentUserService currentUser, NaxapaClock clock, OrderWorkflowService workflow) : Controller
{
    [NaxapaPermission("PEDIDO.VISUALIZAR")]
    public async Task<IActionResult> Index(
        string? status = null,
        string? q = null,
        DateTime? dataInicial = null,
        DateTime? dataFinal = null,
        int pagina = 1,
        CancellationToken cancellationToken = default)
    {
        pagina = Math.Max(1, pagina);
        const int pageSize = 30;
        var est = currentUser.EstabelecimentoId;
        var query =
            from p in db.Pedidos.AsNoTracking()
            join c0 in db.Clientes.AsNoTracking() on p.ClienteId equals c0.ClienteId into clients
            from c in clients.DefaultIfEmpty()
            join u in db.Usuarios.AsNoTracking() on p.CriadoPorUsuarioId equals u.UsuarioId
            where p.EstabelecimentoId == est
            select new { p, Cliente = c == null ? "Balcão" : c.Nome, Operador = u.Nome };

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.p.StatusOperacional == status);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            if (long.TryParse(term, out var numero))
                query = query.Where(x => x.p.Numero == numero || x.Cliente.Contains(term));
            else
                query = query.Where(x => x.Cliente.Contains(term));
        }

        if (dataInicial.HasValue || dataFinal.HasValue)
        {
            var range = clock.GetUtcRange("personalizado", dataInicial, dataFinal ?? dataInicial);
            query = query.Where(x => x.p.CriadoEm >= range.StartUtc && x.p.CriadoEm < range.EndUtc);
        }

        var total = await query.LongCountAsync(cancellationToken);
        var raw = await query.OrderByDescending(x => x.p.CriadoEm)
            .Skip((pagina - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.p.PedidoId, x.p.Numero, x.Cliente, x.p.TipoAtendimento, x.p.StatusOperacional,
                x.p.StatusFinanceiro, x.p.Total, x.p.CriadoEm, x.Operador
            })
            .ToListAsync(cancellationToken);

        return View(new OrderIndexViewModel
        {
            Pedidos = raw.Select(x => new OrderListRow(x.PedidoId, x.Numero, x.Cliente, x.TipoAtendimento,
                x.StatusOperacional, x.StatusFinanceiro, x.Total, clock.ToLocal(x.CriadoEm), x.Operador)).ToList(),
            Status = status,
            Pesquisa = q,
            DataInicial = dataInicial,
            DataFinal = dataFinal,
            Pagina = pagina,
            TotalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)),
            TotalRegistros = total
        });
    }

    [NaxapaPermission("PEDIDO.VISUALIZAR")]
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var order = await (
            from p in db.Pedidos.AsNoTracking()
            join c0 in db.Clientes.AsNoTracking() on p.ClienteId equals c0.ClienteId into clients
            from c in clients.DefaultIfEmpty()
            join u in db.Usuarios.AsNoTracking() on p.CriadoPorUsuarioId equals u.UsuarioId
            where p.PedidoId == id && p.EstabelecimentoId == est
            select new { p, Cliente = c == null ? "Balcão" : c.Nome, Operador = u.Nome })
            .FirstOrDefaultAsync(cancellationToken);
        if (order is null) return NotFound();

        var itemsRaw = await db.PedidoItens.AsNoTracking()
            .Where(x => x.PedidoId == id && x.EstabelecimentoId == est)
            .OrderBy(x => x.PedidoItemId)
            .ToListAsync(cancellationToken);
        var itemIds = itemsRaw.Select(x => x.PedidoItemId).ToList();
        var options = await db.PedidoItemOpcoes.AsNoTracking()
            .Where(x => x.EstabelecimentoId == est && itemIds.Contains(x.PedidoItemId))
            .ToListAsync(cancellationToken);

        var historiesRaw = await (
            from h in db.HistoricosStatusPedido.AsNoTracking()
            join u in db.Usuarios.AsNoTracking() on h.UsuarioId equals u.UsuarioId
            where h.PedidoId == id && h.EstabelecimentoId == est
            orderby h.CriadoEm descending
            select new { h.StatusAnterior, h.StatusNovo, Usuario = u.Nome, h.CriadoEm, h.Observacao })
            .ToListAsync(cancellationToken);

        var vm = new OrderDetailViewModel
        {
            PedidoId = order.p.PedidoId,
            Numero = order.p.Numero,
            Cliente = order.Cliente,
            TipoAtendimento = order.p.TipoAtendimento,
            StatusOperacional = order.p.StatusOperacional,
            StatusFinanceiro = order.p.StatusFinanceiro,
            Subtotal = order.p.Subtotal,
            Desconto = order.p.Desconto,
            Acrescimo = order.p.Acrescimo,
            Total = order.p.Total,
            Operador = order.Operador,
            CriadoEm = clock.ToLocal(order.p.CriadoEm),
            Observacao = order.p.Observacao,
            Itens = itemsRaw.Select(i => new OrderItemDetailViewModel
            {
                PedidoItemId = i.PedidoItemId,
                Produto = i.NomeProduto,
                Quantidade = i.Quantidade,
                ValorUnitario = i.ValorUnitario,
                Desconto = i.Desconto,
                Acrescimo = i.Acrescimo,
                ValorOpcoes = i.ValorOpcoes,
                Total = i.TotalItem,
                Observacao = i.Observacao,
                Opcoes = options.Where(o => o.PedidoItemId == i.PedidoItemId)
                    .Select(o => new OrderOptionRow(o.NomeOpcao, o.Quantidade, o.ValorUnitarioAdicional, o.TotalOpcao)).ToList()
            }).ToList(),
            Historico = historiesRaw.Select(h => new OrderHistoryRow(h.StatusAnterior, h.StatusNovo, h.Usuario, clock.ToLocal(h.CriadoEm), h.Observacao)).ToList()
        };
        return View(vm);
    }

    [NaxapaPermission("PEDIDO.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarStatus(long id, string novoStatus, string? observacao, CancellationToken cancellationToken)
    {
        try
        {
            var printJobs = await workflow.ChangeStatusAsync(currentUser.EstabelecimentoId, id, novoStatus, currentUser.UserId, observacao, cancellationToken);
            TempData["Success"] = printJobs > 0 ? $"Status atualizado e {printJobs} trabalho(s) de impressão enfileirado(s)." : "Status atualizado.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }
}
