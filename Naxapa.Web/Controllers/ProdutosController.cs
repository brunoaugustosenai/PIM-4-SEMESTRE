using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Entities;
using Naxapa.Web.Models;
using Naxapa.Web.Security;
using Naxapa.Web.Services;

namespace Naxapa.Web.Controllers;

[Authorize]
public sealed class ProdutosController(
    NaxapaDbContext db,
    CurrentUserService currentUser,
    FileStorageService fileStorage,
    ProductCsvImportService csvImport,
    Gtin13Service gtin13Service) : Controller
{
    [NaxapaPermission("PRODUTO.VISUALIZAR")]
    public async Task<IActionResult> Index(
        string? q = null,
        long? categoriaId = null,
        bool? ativo = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 10, 100);
        var est = currentUser.EstabelecimentoId;
        var query =
            from p in db.Produtos.AsNoTracking()
            join c in db.Categorias.AsNoTracking() on p.CategoriaId equals c.CategoriaId
            join s0 in db.SetoresProducao.AsNoTracking() on p.SetorProducaoId equals s0.SetorProducaoId into setores
            from s in setores.DefaultIfEmpty()
            where p.EstabelecimentoId == est
            select new { p, Categoria = c.Nome, Setor = s == null ? "Sem setor" : s.Nome };

        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim();
            query = query.Where(x => x.p.Nome.Contains(search)
                || (x.p.CodigoInterno != null && x.p.CodigoInterno.Contains(search))
                || (x.p.CodigoBarras != null && x.p.CodigoBarras.Contains(search))
                || x.Categoria.Contains(search));
        }
        if (categoriaId is not null) query = query.Where(x => x.p.CategoriaId == categoriaId.Value);
        if (ativo is not null) query = query.Where(x => x.p.Ativo == ativo.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.p.Nome)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ProductRow(
                x.p.ProdutoId,
                x.p.CodigoInterno ?? "-",
                x.p.Nome,
                x.Categoria,
                x.p.PrecoVenda,
                x.Setor,
                x.p.DisponivelCardapio,
                x.p.Ativo))
            .ToListAsync(cancellationToken);

        ViewBag.Query = q;
        ViewBag.CategoryId = categoriaId;
        ViewBag.Active = ativo;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.Total = total;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.Categories = await db.Categorias.AsNoTracking().Where(x => x.EstabelecimentoId == est && x.Ativo)
            .OrderBy(x => x.Nome).Select(x => new SelectListItem(x.Nome, x.CategoriaId.ToString(), categoriaId == x.CategoriaId)).ToListAsync(cancellationToken);
        return View(items);
    }


    [HttpGet]
    public async Task<IActionResult> GerarGtin13(CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var gtin = gtin13Service.Generate();
            var exists = await db.Produtos.AsNoTracking()
                .AnyAsync(x => x.EstabelecimentoId == est && x.CodigoBarras == gtin, cancellationToken);
            if (!exists)
                return Json(new { gtin });
        }

        return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Não foi possível gerar um GTIN-13 único. Tente novamente." });
    }

    [NaxapaPermission("PRODUTO.CRIAR")]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new ProductFormViewModel();
        await LoadOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [NaxapaPermission("PRODUTO.CRIAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        await ValidateProductAsync(model, est, null, cancellationToken);
        if (!ModelState.IsValid) { await LoadOptionsAsync(model, cancellationToken); return View(model); }

        string? imageUrl = null;
        try { imageUrl = await fileStorage.SaveImageAsync(model.Imagem, "products", cancellationToken); }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.Imagem), ex.Message);
            await LoadOptionsAsync(model, cancellationToken);
            return View(model);
        }

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var entity = new ProdutoEntity
            {
                EstabelecimentoId = est,
                CategoriaId = model.CategoriaId,
                SetorProducaoId = model.SetorProducaoId,
                CodigoInterno = Clean(model.CodigoInterno),
                CodigoBarras = Clean(model.CodigoBarras),
                Nome = model.Nome.Trim(),
                Descricao = Clean(model.Descricao),
                PrecoVenda = model.PrecoVenda,
                Custo = model.Custo,
                UnidadeMedida = model.UnidadeMedida.Trim().ToUpperInvariant(),
                DisponivelCardapio = model.DisponivelCardapio,
                ControlaEstoque = model.ControlaEstoque,
                Ativo = model.Ativo,
                CriadoEm = DateTime.UtcNow
            };
            db.Produtos.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            if (imageUrl is not null)
            {
                db.ProdutoImagens.Add(new ProdutoImagemEntity { ProdutoId = entity.ProdutoId, Url = imageUrl, Principal = true, OrdemExibicao = 0 });
                await db.SaveChangesAsync(cancellationToken);
            }
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            if (imageUrl is not null) fileStorage.DeleteOwnedFile(imageUrl);
            throw;
        }

        TempData["Success"] = "Produto cadastrado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("PRODUTO.EDITAR")]
    [HttpGet]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var product = await db.Produtos.AsNoTracking().FirstOrDefaultAsync(x => x.ProdutoId == id && x.EstabelecimentoId == est, cancellationToken);
        if (product is null) return NotFound();
        var image = await db.ProdutoImagens.AsNoTracking().Where(x => x.ProdutoId == id && x.Principal).OrderBy(x => x.OrdemExibicao).Select(x => x.Url).FirstOrDefaultAsync(cancellationToken);
        var model = new ProductFormViewModel
        {
            ProdutoId = product.ProdutoId, CodigoInterno = product.CodigoInterno, CodigoBarras = product.CodigoBarras,
            Nome = product.Nome, Descricao = product.Descricao, CategoriaId = product.CategoriaId,
            SetorProducaoId = product.SetorProducaoId, PrecoVenda = product.PrecoVenda, Custo = product.Custo,
            UnidadeMedida = product.UnidadeMedida, DisponivelCardapio = product.DisponivelCardapio,
            ControlaEstoque = product.ControlaEstoque, Ativo = product.Ativo, ImagemAtual = image
        };
        await LoadOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [NaxapaPermission("PRODUTO.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, ProductFormViewModel model, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        await ValidateProductAsync(model, est, id, cancellationToken);
        var product = await db.Produtos.FirstOrDefaultAsync(x => x.ProdutoId == id && x.EstabelecimentoId == est, cancellationToken);
        if (product is null) return NotFound();
        var oldImage = await db.ProdutoImagens.FirstOrDefaultAsync(x => x.ProdutoId == id && x.Principal, cancellationToken);
        model.ImagemAtual = oldImage?.Url;
        if (!ModelState.IsValid) { model.ProdutoId = id; await LoadOptionsAsync(model, cancellationToken); return View(model); }

        string? newImage = null;
        if (model.Imagem is not null)
        {
            try { newImage = await fileStorage.SaveImageAsync(model.Imagem, "products", cancellationToken); }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(model.Imagem), ex.Message); model.ProdutoId = id; await LoadOptionsAsync(model, cancellationToken); return View(model);
            }
        }

        product.CategoriaId = model.CategoriaId; product.SetorProducaoId = model.SetorProducaoId;
        product.CodigoInterno = Clean(model.CodigoInterno); product.CodigoBarras = Clean(model.CodigoBarras);
        product.Nome = model.Nome.Trim(); product.Descricao = Clean(model.Descricao); product.PrecoVenda = model.PrecoVenda;
        product.Custo = model.Custo; product.UnidadeMedida = model.UnidadeMedida.Trim().ToUpperInvariant();
        product.DisponivelCardapio = model.DisponivelCardapio; product.ControlaEstoque = model.ControlaEstoque;
        product.Ativo = model.Ativo; product.AtualizadoEm = DateTime.UtcNow;

        string? previousImage = null;
        if (newImage is not null)
        {
            if (oldImage is null)
                db.ProdutoImagens.Add(new ProdutoImagemEntity { ProdutoId = id, Url = newImage, Principal = true, OrdemExibicao = 0 });
            else
            {
                previousImage = oldImage.Url;
                oldImage.Url = newImage;
            }
        }
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            if (previousImage is not null) fileStorage.DeleteOwnedFile(previousImage);
        }
        catch
        {
            if (newImage is not null) fileStorage.DeleteOwnedFile(newImage);
            throw;
        }
        TempData["Success"] = "Produto atualizado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("PRODUTO.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(long id, CancellationToken cancellationToken)
    {
        var p = await db.Produtos.FirstOrDefaultAsync(x => x.ProdutoId == id && x.EstabelecimentoId == currentUser.EstabelecimentoId, cancellationToken);
        if (p is null) return NotFound();
        p.Ativo = !p.Ativo; p.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = p.Ativo ? "Produto ativado." : "Produto desativado.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("PRODUTO.CRIAR")]
    [HttpGet]
    public IActionResult Import() => View(new ProductImportViewModel());

    [NaxapaPermission("PRODUTO.CRIAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(ProductImportViewModel model, CancellationToken cancellationToken)
    {
        if (model.Arquivo is null) { ModelState.AddModelError(nameof(model.Arquivo), "Selecione um CSV."); return View(model); }
        try
        {
            var token = await csvImport.SaveAsync(model.Arquivo, cancellationToken);
            var parsed = await csvImport.ParseAsync(token, cancellationToken);
            model.ImportToken = token;
            model.Preview = await BuildPreviewAsync(parsed, cancellationToken);
            return View(model);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.Arquivo), ex.Message);
            return View(model);
        }
    }

    [NaxapaPermission("PRODUTO.CRIAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CommitImport(string importToken, CancellationToken cancellationToken)
    {
        try
        {
            var parsed = await csvImport.ParseAsync(importToken, cancellationToken);
            var preview = await BuildPreviewAsync(parsed, cancellationToken);
            if (preview.Any(x => !x.Valido))
            {
                ModelState.AddModelError(string.Empty, "A importação contém erros. Corrija o CSV e gere uma nova pré-visualização.");
                return View("Import", new ProductImportViewModel { ImportToken = importToken, Preview = preview });
            }

            var est = currentUser.EstabelecimentoId;
            var categories = await db.Categorias.Where(x => x.EstabelecimentoId == est).ToListAsync(cancellationToken);
            var sectors = await db.SetoresProducao.Where(x => x.EstabelecimentoId == est).ToListAsync(cancellationToken);
            await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
            foreach (var row in parsed)
            {
                var category = categories.First(x => string.Equals(x.Nome, row.Categoria, StringComparison.OrdinalIgnoreCase));
                var sector = string.IsNullOrWhiteSpace(row.Setor) ? null : sectors.FirstOrDefault(x => string.Equals(x.Nome, row.Setor, StringComparison.OrdinalIgnoreCase));
                db.Produtos.Add(new ProdutoEntity
                {
                    EstabelecimentoId = est, CategoriaId = category.CategoriaId, SetorProducaoId = sector?.SetorProducaoId,
                    CodigoInterno = Clean(row.CodigoInterno), CodigoBarras = Clean(row.CodigoBarras), Nome = row.Nome.Trim(),
                    Descricao = Clean(row.Descricao), PrecoVenda = row.PrecoVenda!.Value, UnidadeMedida = row.UnidadeMedida.Trim().ToUpperInvariant(),
                    DisponivelCardapio = true, ControlaEstoque = false, Ativo = true, CriadoEm = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            csvImport.Delete(importToken);
            TempData["Success"] = $"{parsed.Count} produto(s) importado(s) com sucesso.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Import", new ProductImportViewModel { ImportToken = importToken });
        }
    }

    private async Task<IReadOnlyList<ProductImportRow>> BuildPreviewAsync(IReadOnlyList<ParsedProductCsvRow> rows, CancellationToken ct)
    {
        var est = currentUser.EstabelecimentoId;
        var categories = await db.Categorias.AsNoTracking().Where(x => x.EstabelecimentoId == est && x.Ativo).Select(x => x.Nome).ToListAsync(ct);
        var sectors = await db.SetoresProducao.AsNoTracking().Where(x => x.EstabelecimentoId == est && x.Ativo).Select(x => x.Nome).ToListAsync(ct);
        var codes = await db.Produtos.AsNoTracking().Where(x => x.EstabelecimentoId == est && x.CodigoInterno != null).Select(x => x.CodigoInterno!).ToListAsync(ct);
        var importCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<ProductImportRow>();
        foreach (var row in rows)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(row.Nome)) errors.Add("nome obrigatório");
            if (string.IsNullOrWhiteSpace(row.Categoria) || !categories.Contains(row.Categoria, StringComparer.OrdinalIgnoreCase)) errors.Add("categoria inexistente/inativa");
            if (row.PrecoVenda is null || row.PrecoVenda < 0) errors.Add("preço inválido");
            if (!string.IsNullOrWhiteSpace(row.Setor) && !sectors.Contains(row.Setor, StringComparer.OrdinalIgnoreCase)) errors.Add("setor inexistente/inativo");
            if (!string.IsNullOrWhiteSpace(row.CodigoInterno))
            {
                if (codes.Contains(row.CodigoInterno, StringComparer.OrdinalIgnoreCase)) errors.Add("código já cadastrado");
                if (!importCodes.Add(row.CodigoInterno)) errors.Add("código repetido no CSV");
            }
            result.Add(new ProductImportRow(row.Line, row.CodigoInterno ?? "-", row.Nome, row.Categoria, row.PrecoVenda, row.Setor ?? "-", errors.Count == 0, string.Join("; ", errors)));
        }
        return result;
    }

    private async Task LoadOptionsAsync(ProductFormViewModel model, CancellationToken ct)
    {
        var est = currentUser.EstabelecimentoId;
        model.Categorias = await db.Categorias.AsNoTracking().Where(x => x.EstabelecimentoId == est && x.Ativo)
            .OrderBy(x => x.Nome).Select(x => new SelectListItem(x.Nome, x.CategoriaId.ToString())).ToListAsync(ct);
        model.Setores = await db.SetoresProducao.AsNoTracking().Where(x => x.EstabelecimentoId == est && x.Ativo)
            .OrderBy(x => x.Nome).Select(x => new SelectListItem(x.Nome, x.SetorProducaoId.ToString())).ToListAsync(ct);
    }

    private async Task ValidateProductAsync(ProductFormViewModel model, long est, long? currentId, CancellationToken ct)
    {
        if (!await db.Categorias.AnyAsync(x => x.CategoriaId == model.CategoriaId && x.EstabelecimentoId == est && x.Ativo, ct))
            ModelState.AddModelError(nameof(model.CategoriaId), "Categoria inválida para este estabelecimento.");
        if (model.SetorProducaoId is not null && !await db.SetoresProducao.AnyAsync(x => x.SetorProducaoId == model.SetorProducaoId && x.EstabelecimentoId == est && x.Ativo, ct))
            ModelState.AddModelError(nameof(model.SetorProducaoId), "Setor inválido para este estabelecimento.");
        var code = Clean(model.CodigoInterno);
        if (code is not null && await db.Produtos.AnyAsync(x => x.EstabelecimentoId == est && x.CodigoInterno == code && x.ProdutoId != currentId, ct))
            ModelState.AddModelError(nameof(model.CodigoInterno), "Já existe um produto com este código interno.");
        var bars = Clean(model.CodigoBarras);
        if (bars is not null && bars.Length == 13 && bars.All(char.IsDigit) && !gtin13Service.IsValid(bars))
            ModelState.AddModelError(nameof(model.CodigoBarras), "O GTIN-13 informado possui dígito verificador inválido.");
        if (bars is not null && await db.Produtos.AnyAsync(x => x.EstabelecimentoId == est && x.CodigoBarras == bars && x.ProdutoId != currentId, ct))
            ModelState.AddModelError(nameof(model.CodigoBarras), "Já existe um produto com este código de barras.");
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
