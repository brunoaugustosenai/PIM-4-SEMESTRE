using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Entities;
using Naxapa.Web.Models;
using Naxapa.Web.Security;

namespace Naxapa.Web.Controllers;

[Authorize]
public sealed class SetoresController(NaxapaDbContext db, CurrentUserService currentUser) : Controller
{
    [NaxapaPermission("SETOR.VISUALIZAR")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var rows = await db.SetoresProducao.AsNoTracking()
            .Where(x => x.EstabelecimentoId == est)
            .OrderBy(x => x.Nome)
            .Select(x => new SectorRow(
                x.SetorProducaoId,
                x.Nome,
                x.Descricao ?? "-",
                db.Produtos.Count(p => p.EstabelecimentoId == est && p.SetorProducaoId == x.SetorProducaoId && p.Ativo),
                x.Ativo))
            .ToListAsync(cancellationToken);
        return View(rows);
    }

    [NaxapaPermission("SETOR.EDITAR")]
    [HttpGet]
    public IActionResult Create() => View(new SectorFormViewModel());

    [NaxapaPermission("SETOR.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SectorFormViewModel model, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        if (await db.SetoresProducao.AnyAsync(x => x.EstabelecimentoId == est && x.Nome == model.Nome, cancellationToken))
            ModelState.AddModelError(nameof(model.Nome), "Já existe um setor com este nome.");
        if (!ModelState.IsValid) return View(model);
        db.SetoresProducao.Add(new SetorProducaoEntity
        {
            EstabelecimentoId = est,
            Nome = model.Nome.Trim(),
            Descricao = Clean(model.Descricao),
            Ativo = model.Ativo,
            CriadoEm = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Setor cadastrado.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("SETOR.EDITAR")]
    [HttpGet]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var e = await db.SetoresProducao.AsNoTracking().FirstOrDefaultAsync(x => x.SetorProducaoId == id && x.EstabelecimentoId == currentUser.EstabelecimentoId, cancellationToken);
        if (e is null) return NotFound();
        return View(new SectorFormViewModel { SetorProducaoId = e.SetorProducaoId, Nome = e.Nome, Descricao = e.Descricao, Ativo = e.Ativo });
    }

    [NaxapaPermission("SETOR.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, SectorFormViewModel model, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        if (await db.SetoresProducao.AnyAsync(x => x.EstabelecimentoId == est && x.SetorProducaoId != id && x.Nome == model.Nome, cancellationToken))
            ModelState.AddModelError(nameof(model.Nome), "Já existe um setor com este nome.");
        if (!ModelState.IsValid) { model.SetorProducaoId = id; return View(model); }
        var e = await db.SetoresProducao.FirstOrDefaultAsync(x => x.SetorProducaoId == id && x.EstabelecimentoId == est, cancellationToken);
        if (e is null) return NotFound();
        e.Nome = model.Nome.Trim(); e.Descricao = Clean(model.Descricao); e.Ativo = model.Ativo;
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Setor atualizado.";
        return RedirectToAction(nameof(Index));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
