using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Entities;
using Naxapa.Web.Models;
using Naxapa.Web.Security;

namespace Naxapa.Web.Controllers;

[Authorize]
public sealed class ImpressorasController(NaxapaDbContext db, CurrentUserService currentUser) : Controller
{
    [NaxapaPermission("IMPRESSORA.VISUALIZAR")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var rows = await (
            from i in db.Impressoras.AsNoTracking()
            join s0 in db.SetoresProducao.AsNoTracking() on i.SetorProducaoId equals s0.SetorProducaoId into setores
            from s in setores.DefaultIfEmpty()
            where i.EstabelecimentoId == est
            orderby i.Nome
            select new PrinterRow(
                i.ImpressoraId,
                i.Nome,
                i.Modelo ?? "-",
                i.TipoConexao,
                i.TipoConexao == "REDE" ? ((i.EnderecoIP ?? "-") + (i.Porta == null ? "" : $":{i.Porta}")) : (i.NomeWindows ?? i.TipoConexao),
                s == null ? "Sem setor" : s.Nome,
                i.Padrao,
                i.Ativo))
            .ToListAsync(cancellationToken);
        return View(rows);
    }

    [NaxapaPermission("IMPRESSORA.EDITAR")]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new PrinterFormViewModel();
        await LoadSectorsAsync(model, cancellationToken);
        return View(model);
    }

    [NaxapaPermission("IMPRESSORA.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PrinterFormViewModel model, CancellationToken cancellationToken)
    {
        await ValidateAsync(model, null, cancellationToken);
        if (!ModelState.IsValid) { await LoadSectorsAsync(model, cancellationToken); return View(model); }
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        if (model.Padrao)
            await ClearDefaultAsync(cancellationToken);
        var entity = ToEntity(model, new ImpressoraEntity
        {
            EstabelecimentoId = currentUser.EstabelecimentoId,
            CriadoEm = DateTime.UtcNow
        });
        db.Impressoras.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await SyncProductionConfigurationAsync(entity, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        TempData["Success"] = "Impressora cadastrada.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("IMPRESSORA.EDITAR")]
    [HttpGet]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var e = await db.Impressoras.AsNoTracking().FirstOrDefaultAsync(x => x.ImpressoraId == id && x.EstabelecimentoId == currentUser.EstabelecimentoId, cancellationToken);
        if (e is null) return NotFound();
        var model = new PrinterFormViewModel
        {
            ImpressoraId = e.ImpressoraId, Nome = e.Nome, Modelo = e.Modelo, TipoConexao = e.TipoConexao,
            EnderecoIP = e.EnderecoIP, Porta = e.Porta, NomeWindows = e.NomeWindows, LarguraPapel = e.LarguraPapel,
            SetorProducaoId = e.SetorProducaoId, Padrao = e.Padrao, Ativo = e.Ativo
        };
        await LoadSectorsAsync(model, cancellationToken);
        return View(model);
    }

    [NaxapaPermission("IMPRESSORA.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, PrinterFormViewModel model, CancellationToken cancellationToken)
    {
        await ValidateAsync(model, id, cancellationToken);
        if (!ModelState.IsValid) { model.ImpressoraId = id; await LoadSectorsAsync(model, cancellationToken); return View(model); }
        var e = await db.Impressoras.FirstOrDefaultAsync(x => x.ImpressoraId == id && x.EstabelecimentoId == currentUser.EstabelecimentoId, cancellationToken);
        if (e is null) return NotFound();
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        if (model.Padrao)
            await ClearDefaultAsync(cancellationToken, id);
        ToEntity(model, e);
        await db.SaveChangesAsync(cancellationToken);
        await SyncProductionConfigurationAsync(e, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        TempData["Success"] = "Impressora atualizada.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("IMPRESSORA.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Test(long id, CancellationToken cancellationToken)
    {
        var e = await db.Impressoras.AsNoTracking().FirstOrDefaultAsync(x => x.ImpressoraId == id && x.EstabelecimentoId == currentUser.EstabelecimentoId && x.Ativo, cancellationToken);
        if (e is null) return NotFound();

        var payload = JsonSerializer.Serialize(new
        {
            tipo = "TESTE",
            titulo = "NAXAPA - Teste de impressão",
            impressora = e.Nome,
            solicitadoEmUtc = DateTime.UtcNow,
            mensagem = "Este trabalho precisa ser confirmado pelo agente local de impressão."
        });
        var hash = Guid.NewGuid();
        var tipoDocumento = "RELATORIO";
        await db.Database.ExecuteSqlInterpolatedAsync($@"
EXEC imp.sp_TrabalhoImpressao_Enfileirar
    @EstabelecimentoId={currentUser.EstabelecimentoId},
    @PedidoId=NULL,
    @ImpressoraId={id},
    @SetorProducaoId={e.SetorProducaoId},
    @TipoDocumento={tipoDocumento},
    @PayloadJson={payload},
    @HashIdempotencia={hash};", cancellationToken);

        TempData["Success"] = "Teste enfileirado. O status permanece PENDENTE até um agente local confirmar a impressão; o navegador não marca o trabalho como impresso.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("IMPRESSORA.VISUALIZAR")]
    public async Task<IActionResult> History(CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var rows = await (
            from j in db.TrabalhosImpressao.AsNoTracking()
            join p in db.Impressoras.AsNoTracking() on j.ImpressoraId equals p.ImpressoraId
            where j.EstabelecimentoId == est
            orderby j.SolicitadoEm descending
            select new PrinterJobRow(j.TrabalhoImpressaoId, p.Nome, j.TipoDocumento, j.Status, j.Tentativas,
                j.SolicitadoEm, j.ProcessadoEm, j.UltimoErro, j.PedidoId))
            .Take(300).ToListAsync(cancellationToken);
        return View(rows);
    }

    [NaxapaPermission("IMPRESSORA.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reprint(long id, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var job = await db.TrabalhosImpressao.AsNoTracking().FirstOrDefaultAsync(x => x.TrabalhoImpressaoId == id && x.EstabelecimentoId == est, cancellationToken);
        if (job is null) return NotFound();
        var hash = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($@"
EXEC imp.sp_TrabalhoImpressao_Enfileirar
    @EstabelecimentoId={est}, @PedidoId={job.PedidoId}, @ImpressoraId={job.ImpressoraId},
    @SetorProducaoId={job.SetorProducaoId}, @TipoDocumento={job.TipoDocumento},
    @PayloadJson={job.PayloadJson}, @HashIdempotencia={hash};", cancellationToken);
        TempData["Success"] = "Reimpressão autorizada e adicionada à fila.";
        return RedirectToAction(nameof(History));
    }

    private async Task SyncProductionConfigurationAsync(ImpressoraEntity printer, CancellationToken ct)
    {
        var config = await db.ConfiguracoesImpressao.FirstOrDefaultAsync(x =>
            x.EstabelecimentoId == printer.EstabelecimentoId &&
            x.ImpressoraId == printer.ImpressoraId &&
            x.TipoDocumento == "PRODUCAO", ct);

        if (printer.SetorProducaoId is null || !printer.Ativo)
        {
            if (config is not null) config.Ativo = false;
            return;
        }

        if (config is null)
        {
            config = new ConfiguracaoImpressaoEntity
            {
                EstabelecimentoId = printer.EstabelecimentoId,
                ImpressoraId = printer.ImpressoraId,
                SetorProducaoId = printer.SetorProducaoId,
                TipoDocumento = "PRODUCAO",
                QuantidadeCopias = 1,
                Ativo = true
            };
            db.ConfiguracoesImpressao.Add(config);
        }
        else
        {
            config.SetorProducaoId = printer.SetorProducaoId;
            config.Ativo = true;
            if (config.QuantidadeCopias < 1) config.QuantidadeCopias = 1;
        }
    }

    private async Task ValidateAsync(PrinterFormViewModel model, long? id, CancellationToken ct)
    {
        model.TipoConexao = model.TipoConexao.Trim().ToUpperInvariant();
        if (model.TipoConexao is not ("REDE" or "WINDOWS" or "USB"))
            ModelState.AddModelError(nameof(model.TipoConexao), "Tipo de conexão inválido.");
        if (model.TipoConexao == "REDE" && (string.IsNullOrWhiteSpace(model.EnderecoIP) || model.Porta is null))
            ModelState.AddModelError(nameof(model.EnderecoIP), "Impressora de rede exige endereço IP e porta.");
        if (model.TipoConexao == "WINDOWS" && string.IsNullOrWhiteSpace(model.NomeWindows))
            ModelState.AddModelError(nameof(model.NomeWindows), "Informe o nome da impressora instalada no Windows.");
        if (model.SetorProducaoId is not null && !await db.SetoresProducao.AsNoTracking().AnyAsync(
                x => x.SetorProducaoId == model.SetorProducaoId && x.EstabelecimentoId == currentUser.EstabelecimentoId && x.Ativo, ct))
            ModelState.AddModelError(nameof(model.SetorProducaoId), "Setor inválido.");
        if (await db.Impressoras.AsNoTracking().AnyAsync(x => x.EstabelecimentoId == currentUser.EstabelecimentoId && x.Nome == model.Nome && x.ImpressoraId != id, ct))
            ModelState.AddModelError(nameof(model.Nome), "Já existe uma impressora com este nome.");
    }

    private async Task LoadSectorsAsync(PrinterFormViewModel model, CancellationToken ct)
    {
        model.Setores = await db.SetoresProducao.AsNoTracking()
            .Where(x => x.EstabelecimentoId == currentUser.EstabelecimentoId && x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new SelectListItem(x.Nome, x.SetorProducaoId.ToString()))
            .ToListAsync(ct);
    }

    private async Task ClearDefaultAsync(CancellationToken ct, long? exceptId = null)
    {
        var list = await db.Impressoras.Where(x => x.EstabelecimentoId == currentUser.EstabelecimentoId && x.Padrao && x.ImpressoraId != exceptId).ToListAsync(ct);
        foreach (var printer in list) printer.Padrao = false;
    }

    private static ImpressoraEntity ToEntity(PrinterFormViewModel m, ImpressoraEntity e)
    {
        e.Nome = m.Nome.Trim(); e.Modelo = Clean(m.Modelo); e.TipoConexao = m.TipoConexao;
        e.EnderecoIP = Clean(m.EnderecoIP); e.Porta = m.Porta; e.NomeWindows = Clean(m.NomeWindows);
        e.LarguraPapel = m.LarguraPapel; e.SetorProducaoId = m.SetorProducaoId; e.Padrao = m.Padrao; e.Ativo = m.Ativo;
        return e;
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
