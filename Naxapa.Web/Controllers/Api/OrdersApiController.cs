using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Models;
using Naxapa.Web.Security;
using Naxapa.Web.Services;

namespace Naxapa.Web.Controllers.Api;

[ApiController]
[Route("api/v1/orders")]
[Authorize(AuthenticationSchemes = NaxapaAuthenticationSchemes.ApiBearer)]
[EnableRateLimiting("api")]
public sealed class OrdersApiController(NaxapaDbContext db, CurrentUserService currentUser, OrderWorkflowService workflow) : ControllerBase
{
    [HttpGet]
    [NaxapaPermission("PEDIDO.VISUALIZAR")]
    public async Task<ActionResult<ApiPagedResponse<ApiOrderSummaryDto>>> Get(string? status=null,int page=1,int pageSize=30,CancellationToken cancellationToken=default)
    {
        page=Math.Max(1,page);pageSize=Math.Clamp(pageSize,1,100);var est=currentUser.EstabelecimentoId;
        var q=db.Pedidos.AsNoTracking().Where(x=>x.EstabelecimentoId==est);
        if(!string.IsNullOrWhiteSpace(status))q=q.Where(x=>x.StatusOperacional==status);
        var total=await q.LongCountAsync(cancellationToken);
        var items=await q.OrderByDescending(x=>x.CriadoEm).Skip((page-1)*pageSize).Take(pageSize).Select(x=>new ApiOrderSummaryDto(x.PedidoId,x.Numero,x.StatusOperacional,x.StatusFinanceiro,x.Total,x.CriadoEm)).ToListAsync(cancellationToken);
        return Ok(new ApiPagedResponse<ApiOrderSummaryDto>(items,page,pageSize,total));
    }

    [HttpGet("{id:long}")]
    [NaxapaPermission("PEDIDO.VISUALIZAR")]
    public async Task<ActionResult<ApiOrderSummaryDto>> GetById(long id, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var item = await db.Pedidos.AsNoTracking()
            .Where(x => x.EstabelecimentoId == est && x.PedidoId == id)
            .Select(x => new ApiOrderSummaryDto(x.PedidoId, x.Numero, x.StatusOperacional, x.StatusFinanceiro, x.Total, x.CriadoEm))
            .FirstOrDefaultAsync(cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [NaxapaPermission("PEDIDO.CRIAR")]
    public async Task<ActionResult<ApiOrderSummaryDto>> Create(ApiOrderCreateRequest request,CancellationToken cancellationToken)
    {
        var type=request.ServiceType.Trim().ToUpperInvariant();
        if(type is not ("VENDA_AVULSA" or "BALCAO" or "MESA" or "COMANDA" or "DELIVERY"))return BadRequest(new ProblemDetails{Title="Tipo de atendimento inválido."});
        try
        {
            var rows=await db.Pedidos.FromSqlInterpolated($@"EXEC op.sp_Pedido_Criar @EstabelecimentoId={currentUser.EstabelecimentoId}, @CriadoPorUsuarioId={currentUser.UserId}, @TipoAtendimento={type}, @ClienteId={request.ClientId}, @MesaId={request.TableId}, @ComandaId={request.TabId}, @Observacao={request.Notes}").AsNoTracking().ToListAsync(cancellationToken);
            var p=rows.Single();
            return CreatedAtAction(nameof(GetById),new{id=p.PedidoId},new ApiOrderSummaryDto(p.PedidoId,p.Numero,p.StatusOperacional,p.StatusFinanceiro,p.Total,p.CriadoEm));
        }
        catch(Exception ex){return Conflict(new ProblemDetails{Title="Não foi possível criar o pedido",Detail=ex.InnerException?.Message??ex.Message});}
    }

    [HttpPost("{id:long}/items")]
    [NaxapaPermission("PEDIDO.EDITAR")]
    public async Task<IActionResult> AddItem(long id,ApiOrderItemCreateRequest request,CancellationToken cancellationToken)
    {
        try
        {
            var rows=await db.PedidoItens.FromSqlInterpolated($@"EXEC op.sp_Pedido_AdicionarItem @EstabelecimentoId={currentUser.EstabelecimentoId}, @PedidoId={id}, @ProdutoId={request.ProductId}, @Quantidade={request.Quantity}, @Observacao={request.Notes}").AsNoTracking().ToListAsync(cancellationToken);
            var i=rows.Single();return Ok(new{i.PedidoItemId,i.ProdutoId,i.NomeProduto,i.Quantidade,i.ValorUnitario,i.TotalItem});
        }
        catch(Exception ex){return Conflict(new ProblemDetails{Title="Não foi possível adicionar o item",Detail=ex.InnerException?.Message??ex.Message});}
    }

    [HttpPost("items/{itemId:long}/options")]
    [NaxapaPermission("PEDIDO.EDITAR")]
    public async Task<IActionResult> AddOption(long itemId,ApiOrderOptionCreateRequest request,CancellationToken cancellationToken)
    {
        try
        {
            var rows=await db.PedidoItemOpcoes.FromSqlInterpolated($@"EXEC op.sp_Pedido_AdicionarOpcao @EstabelecimentoId={currentUser.EstabelecimentoId}, @PedidoItemId={itemId}, @OpcaoProdutoId={request.OptionId}, @Quantidade={request.Quantity}").AsNoTracking().ToListAsync(cancellationToken);
            var o=rows.Single();return Ok(new{o.PedidoItemOpcaoId,o.NomeOpcao,o.Quantidade,o.ValorUnitarioAdicional,o.TotalOpcao});
        }
        catch(Exception ex){return Conflict(new ProblemDetails{Title="Não foi possível adicionar a opção",Detail=ex.InnerException?.Message??ex.Message});}
    }

    [HttpPost("{id:long}/status")]
    [NaxapaPermission("PEDIDO.EDITAR")]
    public async Task<IActionResult> Status(long id,ApiStatusRequest request,CancellationToken cancellationToken)
    {
        var status=request.Status.Trim().ToUpperInvariant();
        try
        {
            var printJobs = await workflow.ChangeStatusAsync(currentUser.EstabelecimentoId, id, status, currentUser.UserId, request.Notes, cancellationToken);
            return Ok(new { status, printJobsEnqueued = printJobs });
        }
        catch(Exception ex){return Conflict(new ProblemDetails{Title="Transição de status recusada",Detail=ex.InnerException?.Message??ex.Message});}
    }
}
