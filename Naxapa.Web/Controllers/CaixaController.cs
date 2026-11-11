using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Entities;
using Naxapa.Web.Models;
using Naxapa.Web.Security;
using Naxapa.Web.Services;

namespace Naxapa.Web.Controllers;

public sealed class CaixaController(NaxapaDbContext db, CurrentUserService currentUser, NaxapaClock clock) : Controller
{
    [NaxapaPermission("CAIXA.VISUALIZAR")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var raw = await db.CaixaResumo.AsNoTracking()
            .Where(x => x.EstabelecimentoId == est)
            .OrderByDescending(x => x.AbertoEm).Take(100).ToListAsync(cancellationToken);
        var openIds = raw.Where(x => x.Status == "ABERTO").Select(x => x.CaixaId).ToHashSet();
        var registers = await db.Caixas.AsNoTracking().Where(x => x.EstabelecimentoId == est).OrderBy(x => x.Nome).ToListAsync(cancellationToken);
        return View(new CashIndexViewModel
        {
            Sessoes = raw.Select(x => new CashSessionRow(x.CaixaSessaoId, x.Caixa, x.UsuarioAbertura, x.Status,
                clock.ToLocal(x.AbertoEm), x.ValorAbertura, x.TotalPagamentos, x.Sangrias, x.Suprimentos, x.SaldoFisicoEsperado, x.Diferenca)).ToList(),
            Caixas = registers.Select(x => new CashRegisterRow(x.CaixaId, x.Codigo, x.Nome, x.Descricao ?? "", x.Ativo, openIds.Contains(x.CaixaId))).ToList(),
            SessaoAbertaId = raw.FirstOrDefault(x => x.Status == "ABERTO")?.CaixaSessaoId
        });
    }

    [NaxapaPermission("CAIXA.CADASTRAR")]
    [HttpGet]
    public IActionResult Create() => View(new CashRegisterFormViewModel());

    [NaxapaPermission("CAIXA.CADASTRAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CashRegisterFormViewModel model, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        if (await db.Caixas.AsNoTracking().AnyAsync(x => x.EstabelecimentoId == est && x.Codigo == model.Codigo, cancellationToken))
            ModelState.AddModelError(nameof(model.Codigo), "Código já cadastrado.");
        if (!ModelState.IsValid) return View(model);
        db.Caixas.Add(new CaixaEntity
        {
            EstabelecimentoId = est,
            Codigo = model.Codigo.Trim(), Nome = model.Nome.Trim(), Descricao = Clean(model.Descricao),
            Ativo = model.Ativo, CriadoEm = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Caixa cadastrado.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("CAIXA.ABRIR")]
    [HttpGet]
    public async Task<IActionResult> Abrir(CancellationToken cancellationToken)
    {
        var model = new CashOpenViewModel();
        await LoadRegistersAsync(model, cancellationToken);
        return View(model);
    }

    [NaxapaPermission("CAIXA.ABRIR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Abrir(CashOpenViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { await LoadRegistersAsync(model, cancellationToken); return View(model); }
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync($@"
EXEC fin.sp_Caixa_Abrir
    @EstabelecimentoId={currentUser.EstabelecimentoId},
    @CaixaId={model.CaixaId},
    @UsuarioId={currentUser.UserId},
    @ValorAbertura={model.ValorAbertura},
    @Observacao={model.Observacao};", cancellationToken);
            TempData["Success"] = "Caixa aberto com sucesso.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.InnerException?.Message ?? ex.Message);
            await LoadRegistersAsync(model, cancellationToken);
            return View(model);
        }
    }

    [NaxapaPermission("CAIXA.MOVIMENTAR")]
    [HttpGet]
    public async Task<IActionResult> Movimentar(long id, CancellationToken cancellationToken)
    {
        if (!await db.CaixaSessoes.AsNoTracking().AnyAsync(x => x.CaixaSessaoId == id && x.EstabelecimentoId == currentUser.EstabelecimentoId && x.Status == "ABERTO", cancellationToken))
            return NotFound();
        return View(new CashMovementViewModel { CaixaSessaoId = id });
    }

    [NaxapaPermission("CAIXA.MOVIMENTAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Movimentar(CashMovementViewModel model, CancellationToken cancellationToken)
    {
        if (model.TipoMovimento is not ("SUPRIMENTO" or "SANGRIA" or "AJUSTE_ENTRADA" or "AJUSTE_SAIDA"))
            ModelState.AddModelError(nameof(model.TipoMovimento), "Movimento inválido.");
        if (!ModelState.IsValid) return View(model);
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync($@"
EXEC fin.sp_Caixa_Movimentar
    @EstabelecimentoId={currentUser.EstabelecimentoId},
    @CaixaSessaoId={model.CaixaSessaoId},
    @TipoMovimento={model.TipoMovimento},
    @Valor={model.Valor},
    @UsuarioId={currentUser.UserId},
    @Descricao={model.Descricao};", cancellationToken);
            TempData["Success"] = "Movimentação registrada.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.InnerException?.Message ?? ex.Message);
            return View(model);
        }
    }

    [NaxapaPermission("CAIXA.FECHAR")]
    [HttpGet]
    public async Task<IActionResult> Fechar(long id, CancellationToken cancellationToken)
    {
        if (!await db.CaixaSessoes.AsNoTracking().AnyAsync(x => x.CaixaSessaoId == id && x.EstabelecimentoId == currentUser.EstabelecimentoId && x.Status == "ABERTO", cancellationToken))
            return NotFound();
        return View(new CashCloseViewModel { CaixaSessaoId = id });
    }

    [NaxapaPermission("CAIXA.FECHAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Fechar(CashCloseViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync($@"
EXEC fin.sp_Caixa_Fechar
    @EstabelecimentoId={currentUser.EstabelecimentoId},
    @CaixaSessaoId={model.CaixaSessaoId},
    @UsuarioId={currentUser.UserId},
    @ValorFechamentoInformado={model.ValorFechamentoInformado},
    @Observacao={model.Observacao};", cancellationToken);
            TempData["Success"] = "Caixa fechado e conferido.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.InnerException?.Message ?? ex.Message);
            return View(model);
        }
    }

    private async Task LoadRegistersAsync(CashOpenViewModel model, CancellationToken ct)
    {
        var est = currentUser.EstabelecimentoId;
        var open = await db.CaixaSessoes.AsNoTracking().Where(x => x.EstabelecimentoId == est && x.Status == "ABERTO").Select(x => x.CaixaId).ToListAsync(ct);
        model.Caixas = await db.Caixas.AsNoTracking().Where(x => x.EstabelecimentoId == est && x.Ativo && !open.Contains(x.CaixaId))
            .OrderBy(x => x.Nome).Select(x => new SelectListItem($"{x.Codigo} - {x.Nome}", x.CaixaId.ToString())).ToListAsync(ct);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
