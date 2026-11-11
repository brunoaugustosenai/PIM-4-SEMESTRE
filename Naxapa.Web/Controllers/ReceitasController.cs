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
public sealed class ReceitasController(NaxapaDbContext db, CurrentUserService currentUser) : Controller
{
    [NaxapaPermission("RECEITA.VISUALIZAR")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var rows = await (
            from r in db.Receitas.AsNoTracking()
            join p in db.Produtos.AsNoTracking() on r.ProdutoId equals p.ProdutoId
            where r.EstabelecimentoId == est
            orderby p.Nome
            select new RecipeRow(
                r.ReceitaId,
                p.ProdutoId,
                p.Nome,
                r.Rendimento,
                db.ReceitaItens.Count(i => i.ReceitaId == r.ReceitaId && i.EstabelecimentoId == est),
                r.Ativo))
            .ToListAsync(cancellationToken);
        return View(rows);
    }

    [NaxapaPermission("RECEITA.EDITAR")]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new RecipeFormViewModel();
        await LoadProductsAsync(model, cancellationToken);
        return View(model);
    }

    [NaxapaPermission("RECEITA.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RecipeFormViewModel model, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        if (!await db.Produtos.AsNoTracking().AnyAsync(x => x.ProdutoId == model.ProdutoId && x.EstabelecimentoId == est && x.Ativo, cancellationToken))
            ModelState.AddModelError(nameof(model.ProdutoId), "Produto inválido.");
        if (await db.Receitas.AnyAsync(x => x.EstabelecimentoId == est && x.ProdutoId == model.ProdutoId, cancellationToken))
            ModelState.AddModelError(nameof(model.ProdutoId), "Este produto já possui uma ficha técnica.");
        if (!ModelState.IsValid) { await LoadProductsAsync(model, cancellationToken); return View(model); }
        var e = new ReceitaEntity
        {
            EstabelecimentoId = est, ProdutoId = model.ProdutoId, Rendimento = model.Rendimento,
            Observacao = Clean(model.Observacao), Ativo = model.Ativo, CriadoEm = DateTime.UtcNow
        };
        db.Receitas.Add(e);
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Ficha técnica criada.";
        return RedirectToAction(nameof(Items), new { id = e.ReceitaId });
    }

    [NaxapaPermission("RECEITA.EDITAR")]
    [HttpGet]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var e = await db.Receitas.AsNoTracking().FirstOrDefaultAsync(x => x.ReceitaId == id && x.EstabelecimentoId == currentUser.EstabelecimentoId, cancellationToken);
        if (e is null) return NotFound();
        var model = new RecipeFormViewModel { ReceitaId = e.ReceitaId, ProdutoId = e.ProdutoId, Rendimento = e.Rendimento, Observacao = e.Observacao, Ativo = e.Ativo };
        await LoadProductsAsync(model, cancellationToken);
        return View(model);
    }

    [NaxapaPermission("RECEITA.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, RecipeFormViewModel model, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        if (!await db.Produtos.AsNoTracking().AnyAsync(x => x.ProdutoId == model.ProdutoId && x.EstabelecimentoId == est && x.Ativo, cancellationToken))
            ModelState.AddModelError(nameof(model.ProdutoId), "Produto inválido.");
        if (await db.Receitas.AnyAsync(x => x.EstabelecimentoId == est && x.ProdutoId == model.ProdutoId && x.ReceitaId != id, cancellationToken))
            ModelState.AddModelError(nameof(model.ProdutoId), "Este produto já possui uma ficha técnica.");
        if (!ModelState.IsValid) { model.ReceitaId = id; await LoadProductsAsync(model, cancellationToken); return View(model); }
        var e = await db.Receitas.FirstOrDefaultAsync(x => x.ReceitaId == id && x.EstabelecimentoId == est, cancellationToken);
        if (e is null) return NotFound();
        e.ProdutoId = model.ProdutoId; e.Rendimento = model.Rendimento; e.Observacao = Clean(model.Observacao); e.Ativo = model.Ativo; e.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Ficha técnica atualizada.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("RECEITA.VISUALIZAR")]
    [HttpGet]
    public async Task<IActionResult> Items(long id, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var recipe = await db.Receitas.AsNoTracking().FirstOrDefaultAsync(x => x.ReceitaId == id && x.EstabelecimentoId == est, cancellationToken);
        if (recipe is null) return NotFound();
        var product = await db.Produtos.AsNoTracking().Where(x => x.ProdutoId == recipe.ProdutoId).Select(x => x.Nome).SingleAsync(cancellationToken);
        var items = await (
            from ri in db.ReceitaItens.AsNoTracking()
            join i in db.Insumos.AsNoTracking() on ri.InsumoId equals i.InsumoId
            where ri.ReceitaId == id && ri.EstabelecimentoId == est
            orderby i.Nome
            select new RecipeItemRow(ri.ReceitaItemId, i.Nome, ri.Quantidade, i.UnidadeMedida, i.CustoMedio, i.CustoMedio == null ? null : i.CustoMedio * ri.Quantidade))
            .ToListAsync(cancellationToken);
        ViewBag.RecipeId = id;
        ViewBag.Product = product;
        ViewBag.Rendimento = recipe.Rendimento;
        ViewBag.TotalCost = items.Sum(x => x.CustoItem ?? 0);
        return View(items);
    }

    [NaxapaPermission("RECEITA.EDITAR")]
    [HttpGet]
    public async Task<IActionResult> AddItem(long id, CancellationToken cancellationToken)
    {
        if (!await db.Receitas.AsNoTracking().AnyAsync(x => x.ReceitaId == id && x.EstabelecimentoId == currentUser.EstabelecimentoId, cancellationToken)) return NotFound();
        var model = new RecipeItemFormViewModel { ReceitaId = id };
        await LoadInputsAsync(model, cancellationToken);
        return View(model);
    }

    [NaxapaPermission("RECEITA.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(RecipeItemFormViewModel model, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        if (!await db.Receitas.AsNoTracking().AnyAsync(x => x.ReceitaId == model.ReceitaId && x.EstabelecimentoId == est, cancellationToken)) return NotFound();
        if (!await db.Insumos.AsNoTracking().AnyAsync(x => x.InsumoId == model.InsumoId && x.EstabelecimentoId == est && x.Ativo, cancellationToken))
            ModelState.AddModelError(nameof(model.InsumoId), "Insumo inválido.");
        if (await db.ReceitaItens.AnyAsync(x => x.ReceitaId == model.ReceitaId && x.InsumoId == model.InsumoId, cancellationToken))
            ModelState.AddModelError(nameof(model.InsumoId), "Este insumo já faz parte da ficha técnica.");
        if (!ModelState.IsValid) { await LoadInputsAsync(model, cancellationToken); return View(model); }
        db.ReceitaItens.Add(new ReceitaItemEntity
        {
            EstabelecimentoId = est, ReceitaId = model.ReceitaId, InsumoId = model.InsumoId,
            Quantidade = model.Quantidade, Observacao = Clean(model.Observacao)
        });
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Insumo adicionado à ficha técnica.";
        return RedirectToAction(nameof(Items), new { id = model.ReceitaId });
    }

    [NaxapaPermission("RECEITA.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem(long id, CancellationToken cancellationToken)
    {
        var e = await db.ReceitaItens.FirstOrDefaultAsync(x => x.ReceitaItemId == id && x.EstabelecimentoId == currentUser.EstabelecimentoId, cancellationToken);
        if (e is null) return NotFound();
        var recipeId = e.ReceitaId;
        db.ReceitaItens.Remove(e);
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Insumo removido da ficha técnica.";
        return RedirectToAction(nameof(Items), new { id = recipeId });
    }

    private async Task LoadProductsAsync(RecipeFormViewModel model, CancellationToken ct)
    {
        model.Produtos = await db.Produtos.AsNoTracking().Where(x => x.EstabelecimentoId == currentUser.EstabelecimentoId && x.Ativo)
            .OrderBy(x => x.Nome).Select(x => new SelectListItem(x.Nome, x.ProdutoId.ToString())).ToListAsync(ct);
    }
    private async Task LoadInputsAsync(RecipeItemFormViewModel model, CancellationToken ct)
    {
        model.Insumos = await db.Insumos.AsNoTracking().Where(x => x.EstabelecimentoId == currentUser.EstabelecimentoId && x.Ativo)
            .OrderBy(x => x.Nome).Select(x => new SelectListItem($"{x.Nome} ({x.UnidadeMedida})", x.InsumoId.ToString())).ToListAsync(ct);
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
