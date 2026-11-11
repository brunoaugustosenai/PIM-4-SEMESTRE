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
public sealed class OpcoesController(NaxapaDbContext db, CurrentUserService currentUser) : Controller
{
    [NaxapaPermission("PRODUTO.VISUALIZAR")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var groups = await db.GruposOpcao.AsNoTracking()
            .Where(x => x.EstabelecimentoId == est)
            .OrderBy(x => x.Nome)
            .Select(x => new OptionGroupRow(
                x.GrupoOpcaoId,
                x.Nome,
                x.TipoSelecao == "U" ? "Única" : "Múltipla",
                x.QuantidadeMinima,
                x.QuantidadeMaxima,
                x.Obrigatorio,
                db.OpcoesProduto.Count(o => o.GrupoOpcaoId == x.GrupoOpcaoId && o.EstabelecimentoId == est && o.Ativo),
                x.Ativo))
            .ToListAsync(cancellationToken);

        var options = await (
            from o in db.OpcoesProduto.AsNoTracking()
            join g in db.GruposOpcao.AsNoTracking() on o.GrupoOpcaoId equals g.GrupoOpcaoId
            where o.EstabelecimentoId == est
            orderby g.Nome, o.OrdemExibicao, o.Nome
            select new OptionRow(o.OpcaoProdutoId, g.GrupoOpcaoId, g.Nome, o.Nome, o.ValorAdicional, o.OrdemExibicao, o.Ativo))
            .ToListAsync(cancellationToken);

        ViewBag.Options = options;
        return View(groups);
    }

    [NaxapaPermission("PRODUTO.EDITAR")]
    [HttpGet]
    public IActionResult CreateGroup() => View(new OptionGroupFormViewModel());

    [NaxapaPermission("PRODUTO.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGroup(OptionGroupFormViewModel model, CancellationToken cancellationToken)
    {
        ValidateGroup(model);
        var est = currentUser.EstabelecimentoId;
        if (await db.GruposOpcao.AnyAsync(x => x.EstabelecimentoId == est && x.Nome == model.Nome, cancellationToken))
            ModelState.AddModelError(nameof(model.Nome), "Já existe um grupo com este nome.");
        if (!ModelState.IsValid) return View(model);
        db.GruposOpcao.Add(new GrupoOpcaoEntity
        {
            EstabelecimentoId = est,
            Nome = model.Nome.Trim(),
            Descricao = Clean(model.Descricao),
            TipoSelecao = model.TipoSelecao,
            QuantidadeMinima = model.QuantidadeMinima,
            QuantidadeMaxima = model.QuantidadeMaxima,
            Obrigatorio = model.Obrigatorio,
            Ativo = model.Ativo
        });
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Grupo de opções criado.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("PRODUTO.EDITAR")]
    [HttpGet]
    public async Task<IActionResult> EditGroup(long id, CancellationToken cancellationToken)
    {
        var e = await db.GruposOpcao.AsNoTracking().FirstOrDefaultAsync(x => x.GrupoOpcaoId == id && x.EstabelecimentoId == currentUser.EstabelecimentoId, cancellationToken);
        if (e is null) return NotFound();
        return View(new OptionGroupFormViewModel
        {
            GrupoOpcaoId = e.GrupoOpcaoId, Nome = e.Nome, Descricao = e.Descricao, TipoSelecao = e.TipoSelecao,
            QuantidadeMinima = e.QuantidadeMinima, QuantidadeMaxima = e.QuantidadeMaxima, Obrigatorio = e.Obrigatorio, Ativo = e.Ativo
        });
    }

    [NaxapaPermission("PRODUTO.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditGroup(long id, OptionGroupFormViewModel model, CancellationToken cancellationToken)
    {
        ValidateGroup(model);
        var est = currentUser.EstabelecimentoId;
        if (await db.GruposOpcao.AnyAsync(x => x.EstabelecimentoId == est && x.GrupoOpcaoId != id && x.Nome == model.Nome, cancellationToken))
            ModelState.AddModelError(nameof(model.Nome), "Já existe um grupo com este nome.");
        if (!ModelState.IsValid) { model.GrupoOpcaoId = id; return View(model); }
        var e = await db.GruposOpcao.FirstOrDefaultAsync(x => x.GrupoOpcaoId == id && x.EstabelecimentoId == est, cancellationToken);
        if (e is null) return NotFound();
        e.Nome = model.Nome.Trim(); e.Descricao = Clean(model.Descricao); e.TipoSelecao = model.TipoSelecao;
        e.QuantidadeMinima = model.QuantidadeMinima; e.QuantidadeMaxima = model.QuantidadeMaxima; e.Obrigatorio = model.Obrigatorio; e.Ativo = model.Ativo;
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Grupo atualizado.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("PRODUTO.EDITAR")]
    [HttpGet]
    public async Task<IActionResult> CreateOption(long groupId, CancellationToken cancellationToken)
    {
        if (!await GroupExistsAsync(groupId, cancellationToken)) return NotFound();
        return View(new OptionFormViewModel { GrupoOpcaoId = groupId });
    }

    [NaxapaPermission("PRODUTO.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOption(OptionFormViewModel model, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        if (!await GroupExistsAsync(model.GrupoOpcaoId, cancellationToken)) ModelState.AddModelError(nameof(model.GrupoOpcaoId), "Grupo inválido.");
        if (await db.OpcoesProduto.AnyAsync(x => x.EstabelecimentoId == est && x.GrupoOpcaoId == model.GrupoOpcaoId && x.Nome == model.Nome, cancellationToken))
            ModelState.AddModelError(nameof(model.Nome), "Já existe uma opção com este nome no grupo.");
        if (!ModelState.IsValid) return View(model);
        db.OpcoesProduto.Add(new OpcaoProdutoEntity
        {
            EstabelecimentoId = est, GrupoOpcaoId = model.GrupoOpcaoId, Nome = model.Nome.Trim(),
            ValorAdicional = model.ValorAdicional, OrdemExibicao = model.OrdemExibicao, Ativo = model.Ativo
        });
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Opção cadastrada.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("PRODUTO.EDITAR")]
    [HttpGet]
    public async Task<IActionResult> EditOption(long id, CancellationToken cancellationToken)
    {
        var e = await db.OpcoesProduto.AsNoTracking().FirstOrDefaultAsync(x => x.OpcaoProdutoId == id && x.EstabelecimentoId == currentUser.EstabelecimentoId, cancellationToken);
        if (e is null) return NotFound();
        return View(new OptionFormViewModel
        {
            OpcaoProdutoId = e.OpcaoProdutoId, GrupoOpcaoId = e.GrupoOpcaoId, Nome = e.Nome,
            ValorAdicional = e.ValorAdicional, OrdemExibicao = e.OrdemExibicao, Ativo = e.Ativo
        });
    }

    [NaxapaPermission("PRODUTO.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditOption(long id, OptionFormViewModel model, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        if (!await GroupExistsAsync(model.GrupoOpcaoId, cancellationToken)) ModelState.AddModelError(nameof(model.GrupoOpcaoId), "Grupo inválido.");
        if (await db.OpcoesProduto.AnyAsync(x => x.EstabelecimentoId == est && x.OpcaoProdutoId != id && x.GrupoOpcaoId == model.GrupoOpcaoId && x.Nome == model.Nome, cancellationToken))
            ModelState.AddModelError(nameof(model.Nome), "Já existe uma opção com este nome no grupo.");
        if (!ModelState.IsValid) { model.OpcaoProdutoId = id; return View(model); }
        var e = await db.OpcoesProduto.FirstOrDefaultAsync(x => x.OpcaoProdutoId == id && x.EstabelecimentoId == est, cancellationToken);
        if (e is null) return NotFound();
        e.GrupoOpcaoId = model.GrupoOpcaoId; e.Nome = model.Nome.Trim(); e.ValorAdicional = model.ValorAdicional; e.OrdemExibicao = model.OrdemExibicao; e.Ativo = model.Ativo;
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Opção atualizada.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("PRODUTO.EDITAR")]
    [HttpGet]
    public async Task<IActionResult> Products(long id, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var group = await db.GruposOpcao.AsNoTracking().FirstOrDefaultAsync(x => x.GrupoOpcaoId == id && x.EstabelecimentoId == est, cancellationToken);
        if (group is null) return NotFound();
        var selected = await db.ProdutoGruposOpcao.AsNoTracking().Where(x => x.GrupoOpcaoId == id && x.EstabelecimentoId == est).Select(x => x.ProdutoId).ToListAsync(cancellationToken);
        var products = await db.Produtos.AsNoTracking().Where(x => x.EstabelecimentoId == est && x.Ativo).OrderBy(x => x.Nome).ToListAsync(cancellationToken);
        return View(new OptionGroupProductsViewModel
        {
            GrupoOpcaoId = id,
            Grupo = group.Nome,
            ProdutoIds = selected,
            Produtos = products.Select(x => new SelectListItem(x.Nome, x.ProdutoId.ToString(), selected.Contains(x.ProdutoId))).ToList()
        });
    }

    [NaxapaPermission("PRODUTO.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Products(long id, OptionGroupProductsViewModel model, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        if (!await GroupExistsAsync(id, cancellationToken)) return NotFound();
        var valid = await db.Produtos.AsNoTracking().Where(x => x.EstabelecimentoId == est && x.Ativo).Select(x => x.ProdutoId).ToListAsync(cancellationToken);
        var selected = model.ProdutoIds.Distinct().Where(valid.Contains).ToList();
        var old = await db.ProdutoGruposOpcao.Where(x => x.EstabelecimentoId == est && x.GrupoOpcaoId == id).ToListAsync(cancellationToken);
        db.ProdutoGruposOpcao.RemoveRange(old);
        var order = 0;
        foreach (var productId in selected)
            db.ProdutoGruposOpcao.Add(new ProdutoGrupoOpcaoEntity { EstabelecimentoId = est, GrupoOpcaoId = id, ProdutoId = productId, OrdemExibicao = order++ });
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Vínculos do grupo com produtos atualizados.";
        return RedirectToAction(nameof(Index));
    }

    private Task<bool> GroupExistsAsync(long id, CancellationToken ct) => db.GruposOpcao.AsNoTracking().AnyAsync(x => x.GrupoOpcaoId == id && x.EstabelecimentoId == currentUser.EstabelecimentoId, ct);
    private void ValidateGroup(OptionGroupFormViewModel m)
    {
        m.TipoSelecao = m.TipoSelecao.Trim().ToUpperInvariant();
        if (m.TipoSelecao is not ("U" or "M")) ModelState.AddModelError(nameof(m.TipoSelecao), "Tipo de seleção inválido.");
        if (m.QuantidadeMaxima < m.QuantidadeMinima) ModelState.AddModelError(nameof(m.QuantidadeMaxima), "Quantidade máxima deve ser maior ou igual à mínima.");
        if (m.TipoSelecao == "U" && m.QuantidadeMaxima > 1) ModelState.AddModelError(nameof(m.QuantidadeMaxima), "Seleção única permite no máximo uma opção.");
        if (m.Obrigatorio && m.QuantidadeMinima == 0) ModelState.AddModelError(nameof(m.QuantidadeMinima), "Grupo obrigatório deve exigir pelo menos uma seleção.");
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
