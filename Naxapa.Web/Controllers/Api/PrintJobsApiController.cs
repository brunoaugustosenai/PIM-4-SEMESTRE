using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Models;
using Naxapa.Web.Security;

namespace Naxapa.Web.Controllers.Api;

[ApiController]
[Route("api/v1/print-jobs")]
[Authorize(AuthenticationSchemes = NaxapaAuthenticationSchemes.ApiBearer)]
[EnableRateLimiting("api")]
public sealed class PrintJobsApiController(NaxapaDbContext db, CurrentUserService currentUser) : ControllerBase
{

    [HttpGet("printers")]
    [NaxapaPermission("IMPRESSORA.VISUALIZAR")]
    public async Task<ActionResult<IReadOnlyList<ApiPrinterDto>>> Printers(CancellationToken cancellationToken)
        => Ok(await db.Impressoras.AsNoTracking()
            .Where(x => x.EstabelecimentoId == currentUser.EstabelecimentoId && x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new ApiPrinterDto(x.ImpressoraId, x.Nome, x.Modelo, x.TipoConexao, x.EnderecoIP, x.Porta, x.NomeWindows, x.LarguraPapel, x.Padrao, x.Ativo))
            .ToListAsync(cancellationToken));

    [HttpGet("pending")]
    [NaxapaPermission("IMPRESSORA.VISUALIZAR")]
    public async Task<ActionResult<IReadOnlyList<ApiPrintJobDto>>> Pending(int limit = 20, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        var est = currentUser.EstabelecimentoId;
        var rows = await db.TrabalhosImpressao.AsNoTracking()
            .Where(x => x.EstabelecimentoId == est && (x.Status == "PENDENTE" || x.Status == "ERRO") && x.Tentativas < 5)
            .OrderBy(x => x.SolicitadoEm)
            .Take(limit)
            .Select(x => new ApiPrintJobDto(x.TrabalhoImpressaoId, x.PedidoId, x.ImpressoraId, x.SetorProducaoId, x.TipoDocumento, x.Status, x.Tentativas, x.HashIdempotencia, x.PayloadJson, x.SolicitadoEm, x.UltimoErro))
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpPost("{id:long}/claim")]
    [NaxapaPermission("IMPRESSORA.EDITAR")]
    public async Task<IActionResult> Claim(long id, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var affected = await db.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE imp.TrabalhoImpressao
SET Status = 'PROCESSANDO', Tentativas = Tentativas + 1, UltimoErro = NULL
WHERE TrabalhoImpressaoId = {id}
  AND EstabelecimentoId = {est}
  AND Status IN ('PENDENTE','ERRO')
  AND Tentativas < 5", cancellationToken);
        return affected == 1 ? NoContent() : Conflict(new ProblemDetails { Title = "O trabalho não está disponível para processamento." });
    }

    [HttpPost("{id:long}/complete")]
    [NaxapaPermission("IMPRESSORA.EDITAR")]
    public async Task<IActionResult> Complete(long id, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var affected = await db.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE imp.TrabalhoImpressao
SET Status = 'IMPRESSO', ProcessadoEm = SYSUTCDATETIME(), UltimoErro = NULL
WHERE TrabalhoImpressaoId = {id} AND EstabelecimentoId = {est} AND Status = 'PROCESSANDO'", cancellationToken);
        return affected == 1 ? NoContent() : Conflict(new ProblemDetails { Title = "O trabalho não estava em processamento." });
    }

    [HttpPost("{id:long}/fail")]
    [NaxapaPermission("IMPRESSORA.EDITAR")]
    public async Task<IActionResult> Fail(long id, ApiPrintFailRequest request, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var error = request.Error.Trim();
        if (error.Length > 1000) error = error[..1000];
        var affected = await db.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE imp.TrabalhoImpressao
SET Status = 'ERRO', UltimoErro = {error}
WHERE TrabalhoImpressaoId = {id} AND EstabelecimentoId = {est} AND Status = 'PROCESSANDO'", cancellationToken);
        return affected == 1 ? NoContent() : Conflict(new ProblemDetails { Title = "O trabalho não estava em processamento." });
    }
}
