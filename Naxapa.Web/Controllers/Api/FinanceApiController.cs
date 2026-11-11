using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Models;
using Naxapa.Web.Security;

namespace Naxapa.Web.Controllers.Api;

[ApiController]
[Route("api/v1")]
[Authorize(AuthenticationSchemes = NaxapaAuthenticationSchemes.ApiBearer)]
[EnableRateLimiting("api")]
public sealed class FinanceApiController(NaxapaDbContext db,CurrentUserService currentUser):ControllerBase
{
    [HttpGet("payment-methods")]
    [NaxapaPermission("PEDIDO.VISUALIZAR")]
    public async Task<ActionResult<IReadOnlyList<ApiPaymentMethodDto>>> Methods(CancellationToken cancellationToken)
        =>Ok(await db.FormasPagamento.AsNoTracking().Where(x=>x.EstabelecimentoId==currentUser.EstabelecimentoId&&x.Ativo).OrderBy(x=>x.Nome).Select(x=>new ApiPaymentMethodDto(x.FormaPagamentoId,x.Codigo,x.Nome,x.Tipo,x.PermiteTroco)).ToListAsync(cancellationToken));

    [HttpPost("orders/{orderId:long}/payments")]
    [NaxapaPermission("PAGAMENTO.REGISTRAR")]
    public async Task<IActionResult> Pay(long orderId,ApiPaymentRequest request,CancellationToken cancellationToken)
    {
        try
        {
            var rows=await db.Pagamentos.FromSqlInterpolated($@"EXEC fin.sp_Pagamento_Registrar @EstabelecimentoId={currentUser.EstabelecimentoId}, @PedidoId={orderId}, @FormaPagamentoId={request.PaymentMethodId}, @Valor={request.Value}, @CriadoPorUsuarioId={currentUser.UserId}, @ValorRecebido={request.ReceivedValue}, @ReferenciaTransacao={request.TransactionReference}, @CaixaSessaoId={request.CashSessionId}").AsNoTracking().ToListAsync(cancellationToken);
            var p=rows.Single();return Ok(new{p.PagamentoId,p.Valor,p.ValorRecebido,p.Troco,p.Status});
        }
        catch(Exception ex){return Conflict(new ProblemDetails{Title="Pagamento recusado",Detail=ex.InnerException?.Message??ex.Message});}
    }


    [HttpGet("cash-registers")]
    [NaxapaPermission("CAIXA.VISUALIZAR")]
    public async Task<ActionResult<IReadOnlyList<ApiCashRegisterDto>>> CashRegisters(CancellationToken cancellationToken)
        => Ok(await db.Caixas.AsNoTracking().Where(x => x.EstabelecimentoId == currentUser.EstabelecimentoId && x.Ativo)
            .OrderBy(x => x.Nome).Select(x => new ApiCashRegisterDto(x.CaixaId, x.Codigo, x.Nome, x.Descricao, x.Ativo)).ToListAsync(cancellationToken));

    [HttpGet("cash")]
    [NaxapaPermission("CAIXA.VISUALIZAR")]
    public async Task<IActionResult> Cash(CancellationToken cancellationToken)
        =>Ok(await db.CaixaResumo.AsNoTracking().Where(x=>x.EstabelecimentoId==currentUser.EstabelecimentoId).OrderByDescending(x=>x.AbertoEm).Take(100).ToListAsync(cancellationToken));

    [HttpPost("cash/open")]
    [NaxapaPermission("CAIXA.ABRIR")]
    public async Task<IActionResult> Open(ApiCashOpenRequest request,CancellationToken cancellationToken)
    {
        try{await db.Database.ExecuteSqlInterpolatedAsync($@"EXEC fin.sp_Caixa_Abrir @EstabelecimentoId={currentUser.EstabelecimentoId}, @CaixaId={request.CashRegisterId}, @UsuarioId={currentUser.UserId}, @ValorAbertura={request.OpeningValue}, @Observacao={request.Notes}",cancellationToken);return NoContent();}
        catch(Exception ex){return Conflict(new ProblemDetails{Title="Abertura recusada",Detail=ex.InnerException?.Message??ex.Message});}
    }

    [HttpPost("cash/{sessionId:long}/movements")]
    [NaxapaPermission("CAIXA.MOVIMENTAR")]
    public async Task<IActionResult> Move(long sessionId,ApiCashMoveRequest request,CancellationToken cancellationToken)
    {
        var type=request.Type.Trim().ToUpperInvariant();if(type is not ("SUPRIMENTO" or "SANGRIA" or "AJUSTE_ENTRADA" or "AJUSTE_SAIDA"))return BadRequest(new ProblemDetails{Title="Movimento inválido"});
        try{await db.Database.ExecuteSqlInterpolatedAsync($@"EXEC fin.sp_Caixa_Movimentar @EstabelecimentoId={currentUser.EstabelecimentoId}, @CaixaSessaoId={sessionId}, @TipoMovimento={type}, @Valor={request.Value}, @UsuarioId={currentUser.UserId}, @Descricao={request.Description}",cancellationToken);return NoContent();}
        catch(Exception ex){return Conflict(new ProblemDetails{Title="Movimentação recusada",Detail=ex.InnerException?.Message??ex.Message});}
    }

    [HttpPost("cash/{sessionId:long}/close")]
    [NaxapaPermission("CAIXA.FECHAR")]
    public async Task<IActionResult> Close(long sessionId,ApiCashCloseRequest request,CancellationToken cancellationToken)
    {
        try{await db.Database.ExecuteSqlInterpolatedAsync($@"EXEC fin.sp_Caixa_Fechar @EstabelecimentoId={currentUser.EstabelecimentoId}, @CaixaSessaoId={sessionId}, @UsuarioId={currentUser.UserId}, @ValorFechamentoInformado={request.CountedValue}, @Observacao={request.Notes}",cancellationToken);return NoContent();}
        catch(Exception ex){return Conflict(new ProblemDetails{Title="Fechamento recusado",Detail=ex.InnerException?.Message??ex.Message});}
    }
}
