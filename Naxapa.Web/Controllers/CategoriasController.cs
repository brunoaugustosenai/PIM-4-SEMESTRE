using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Entities;
using Naxapa.Web.Models;
using Naxapa.Web.Security;
using Naxapa.Web.Services;

namespace Naxapa.Web.Controllers;

[Authorize]
public sealed class CategoriasController(
    NaxapaDbContext db,
    CurrentUserService currentUser,
    FileStorageService fileStorage) : Controller
{
    [NaxapaPermission("CATEGORIA.VISUALIZAR")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var rows = await db.Categorias.AsNoTracking()
            .Where(x => x.EstabelecimentoId == est)
            .OrderBy(x => x.OrdemExibicao).ThenBy(x => x.Nome)
            .Select(x => new CategoryRow(
                x.CategoriaId,
                x.Nome,
                x.OrdemExibicao,
                db.Produtos.Count(p => p.EstabelecimentoId == est && p.CategoriaId == x.CategoriaId && p.Ativo),
                x.Ativo,
                x.ImagemUrl))
            .ToListAsync(cancellationToken);
        return View(rows);
    }

    [NaxapaPermission("CATEGORIA.EDITAR")]
    [HttpGet]
    public IActionResult Create() => View(new CategoryFormViewModel());

    [NaxapaPermission("CATEGORIA.EDITAR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        if (await db.Categorias.AnyAsync(x => x.EstabelecimentoId == est && x.Nome == model.Nome, cancellationToken))
            ModelState.AddModelError(nameof(model.Nome), "Já existe uma categoria com este nome.");
        if (!ModelState.IsValid) return View(model);

        string? image = null;
        try { image = await fileStorage.SaveImageAsync(model.Imagem, "categories", cancellationToken); }
        catch (InvalidOperationException ex) { ModelState.AddModelError(nameof(model.Imagem), ex.Message); return View(model); }

        db.Categorias.Add(new CategoriaEntity
        {
            EstabelecimentoId = est,
            Nome = model.Nome.Trim(),
            Descricao = Clean(model.Descricao),
            ImagemUrl = image,
            OrdemExibicao = model.OrdemExibicao,
            Ativo = model.Ativo,
            CriadoEm = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Categoria cadastrada.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("CATEGORIA.EDITAR")]
    [HttpGet]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var entity = await db.Categorias.AsNoTracking().FirstOrDefaultAsync(x => x.CategoriaId == id && x.EstabelecimentoId == est, cancellationToken);
        if (entity is null) return NotFound();
        return View(new CategoryFormViewModel
        {
            CategoriaId = entity.CategoriaId,
            Nome = entity.Nome,
            Descricao = entity.Descricao,
            ImagemAtual = entity.ImagemUrl,
            OrdemExibicao = entity.OrdemExibicao,
            Ativo = entity.Ativo
        });
    }

    [NaxapaPermission("CATEGORIA.EDITAR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        if (await db.Categorias.AnyAsync(x => x.EstabelecimentoId == est && x.CategoriaId != id && x.Nome == model.Nome, cancellationToken))
            ModelState.AddModelError(nameof(model.Nome), "Já existe uma categoria com este nome.");
        if (!ModelState.IsValid) { model.CategoriaId = id; return View(model); }

        var entity = await db.Categorias.FirstOrDefaultAsync(x => x.CategoriaId == id && x.EstabelecimentoId == est, cancellationToken);
        if (entity is null) return NotFound();

        string? newImage = null;
        var previousImage = entity.ImagemUrl;
        if (model.Imagem is not null)
        {
            try
            {
                newImage = await fileStorage.SaveImageAsync(model.Imagem, "categories", cancellationToken);
                if (newImage is not null) entity.ImagemUrl = newImage;
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(model.Imagem), ex.Message);
                model.ImagemAtual = entity.ImagemUrl;
                return View(model);
            }
        }

        entity.Nome = model.Nome.Trim();
        entity.Descricao = Clean(model.Descricao);
        entity.OrdemExibicao = model.OrdemExibicao;
        entity.Ativo = model.Ativo;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            if (newImage is not null && previousImage is not null) fileStorage.DeleteOwnedFile(previousImage);
        }
        catch
        {
            if (newImage is not null) fileStorage.DeleteOwnedFile(newImage);
            throw;
        }
        TempData["Success"] = "Categoria atualizada.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("CATEGORIA.EDITAR")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(long id, CancellationToken cancellationToken)
    {
        var entity = await db.Categorias.FirstOrDefaultAsync(x => x.CategoriaId == id && x.EstabelecimentoId == currentUser.EstabelecimentoId, cancellationToken);
        if (entity is null) return NotFound();
        entity.Ativo = !entity.Ativo;
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = entity.Ativo ? "Categoria ativada." : "Categoria desativada.";
        return RedirectToAction(nameof(Index));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
