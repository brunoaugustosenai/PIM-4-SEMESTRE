using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Entities;
using Naxapa.Web.Models;
using Naxapa.Web.Security;

namespace Naxapa.Web.Controllers;

[Authorize]
public sealed class CardapioController(NaxapaDbContext db, CurrentUserService currentUser) : Controller
{
    [NaxapaPermission("CARDAPIO.VISUALIZAR")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var rows = await db.Cardapios.AsNoTracking()
            .Where(x => x.EstabelecimentoId == est)
            .OrderBy(x => x.Nome)
            .Select(x => new MenuRow(
                x.CardapioId,
                x.Nome,
                db.CardapioProdutos.Count(cp => cp.EstabelecimentoId == est && cp.CardapioId == x.CardapioId && cp.Disponivel),
                x.Publicado,
                x.Ativo,
                x.DataInicio,
                x.DataFim))
            .ToListAsync(cancellationToken);
        return View(rows);
    }

    [NaxapaPermission("CARDAPIO.EDITAR")]
    [HttpGet]
    public IActionResult Create() => View(new MenuFormViewModel());

    [NaxapaPermission("CARDAPIO.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MenuFormViewModel model, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        await ValidateAsync(model, null, cancellationToken);
        if (!ModelState.IsValid) return View(model);
        var e = new CardapioEntity
        {
            EstabelecimentoId = est,
            Nome = model.Nome.Trim(),
            Descricao = Clean(model.Descricao),
            DataInicio = model.DataInicio,
            DataFim = model.DataFim,
            Publicado = model.Publicado,
            Ativo = model.Ativo,
            CriadoEm = DateTime.UtcNow
        };
        db.Cardapios.Add(e);
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Cardápio cadastrado.";
        return RedirectToAction(nameof(Products), new { id = e.CardapioId });
    }

    [NaxapaPermission("CARDAPIO.EDITAR")]
    [HttpGet]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var e = await db.Cardapios.AsNoTracking().FirstOrDefaultAsync(x => x.CardapioId == id && x.EstabelecimentoId == currentUser.EstabelecimentoId, cancellationToken);
        if (e is null) return NotFound();
        return View(new MenuFormViewModel
        {
            CardapioId = e.CardapioId,
            Nome = e.Nome,
            Descricao = e.Descricao,
            DataInicio = e.DataInicio,
            DataFim = e.DataFim,
            Publicado = e.Publicado,
            Ativo = e.Ativo
        });
    }

    [NaxapaPermission("CARDAPIO.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, MenuFormViewModel model, CancellationToken cancellationToken)
    {
        await ValidateAsync(model, id, cancellationToken);
        if (!ModelState.IsValid) { model.CardapioId = id; return View(model); }
        var e = await db.Cardapios.FirstOrDefaultAsync(x => x.CardapioId == id && x.EstabelecimentoId == currentUser.EstabelecimentoId, cancellationToken);
        if (e is null) return NotFound();
        e.Nome = model.Nome.Trim(); e.Descricao = Clean(model.Descricao); e.DataInicio = model.DataInicio; e.DataFim = model.DataFim;
        e.Publicado = model.Publicado; e.Ativo = model.Ativo;
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Cardápio atualizado.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("CARDAPIO.EDITAR")]
    [HttpGet]
    public async Task<IActionResult> Products(long id, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var menu = await db.Cardapios.AsNoTracking().FirstOrDefaultAsync(x => x.CardapioId == id && x.EstabelecimentoId == est, cancellationToken);
        if (menu is null) return NotFound();
        var selected = await db.CardapioProdutos.AsNoTracking().Where(x => x.CardapioId == id && x.EstabelecimentoId == est).ToListAsync(cancellationToken);
        var products = await (
            from p in db.Produtos.AsNoTracking()
            join c in db.Categorias.AsNoTracking() on p.CategoriaId equals c.CategoriaId
            where p.EstabelecimentoId == est && p.Ativo
            orderby c.OrdemExibicao, c.Nome, p.Nome
            select new { p, Categoria = c.Nome }).ToListAsync(cancellationToken);

        return View(new MenuProductsViewModel
        {
            CardapioId = id,
            Cardapio = menu.Nome,
            ProdutoIds = selected.Where(x => x.Disponivel).Select(x => x.ProdutoId).ToList(),
            Produtos = products.Select(x =>
            {
                var cp = selected.FirstOrDefault(s => s.ProdutoId == x.p.ProdutoId);
                return new MenuProductChoice(x.p.ProdutoId, x.p.Nome, x.Categoria, x.p.PrecoVenda, cp?.Disponivel == true, cp?.Disponivel == true, cp?.OrdemExibicao ?? 0);
            }).ToList()
        });
    }

    [NaxapaPermission("CARDAPIO.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Products(long id, MenuProductsViewModel model, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        if (!await db.Cardapios.AnyAsync(x => x.CardapioId == id && x.EstabelecimentoId == est, cancellationToken))
            return NotFound();

        var validProductIds = await db.Produtos.AsNoTracking()
            .Where(x => x.EstabelecimentoId == est && x.Ativo)
            .Select(x => x.ProdutoId)
            .ToListAsync(cancellationToken);
        var selected = model.ProdutoIds.Distinct().Where(validProductIds.Contains).ToHashSet();

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        var existing = await db.CardapioProdutos.Where(x => x.CardapioId == id && x.EstabelecimentoId == est).ToListAsync(cancellationToken);
        var existingByProduct = existing.ToDictionary(x => x.ProdutoId);
        var order = 0;
        foreach (var productId in validProductIds)
        {
            if (existingByProduct.TryGetValue(productId, out var cp))
            {
                cp.Disponivel = selected.Contains(productId);
                if (cp.Disponivel) cp.OrdemExibicao = order++;
            }
            else if (selected.Contains(productId))
            {
                db.CardapioProdutos.Add(new CardapioProdutoEntity
                {
                    EstabelecimentoId = est,
                    CardapioId = id,
                    ProdutoId = productId,
                    OrdemExibicao = order++,
                    Disponivel = true
                });
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        TempData["Success"] = "Produtos do cardápio atualizados.";
        return RedirectToAction(nameof(Products), new { id });
    }

    private async Task ValidateAsync(MenuFormViewModel model, long? currentId, CancellationToken ct)
    {
        if (model.DataInicio is not null && model.DataFim is not null && model.DataFim < model.DataInicio)
            ModelState.AddModelError(nameof(model.DataFim), "A data final não pode ser anterior à inicial.");
        if (await db.Cardapios.AnyAsync(x => x.EstabelecimentoId == currentUser.EstabelecimentoId && x.Nome == model.Nome && x.CardapioId != currentId, ct))
            ModelState.AddModelError(nameof(model.Nome), "Já existe um cardápio com este nome.");
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
