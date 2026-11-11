using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Entities;
using Naxapa.Web.Models;
using Naxapa.Web.Security;

namespace Naxapa.Web.Controllers;

[Authorize]
public sealed class InsumosController(NaxapaDbContext db, CurrentUserService currentUser) : Controller
{
    [NaxapaPermission("INSUMO.VISUALIZAR")]
    public async Task<IActionResult> Index(string? q = null, CancellationToken cancellationToken = default)
    {
        var est = currentUser.EstabelecimentoId;
        var query = db.Insumos.AsNoTracking().Where(x => x.EstabelecimentoId == est);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim();
            query = query.Where(x => x.Nome.Contains(search) || (x.CodigoInterno != null && x.CodigoInterno.Contains(search)));
        }
        var rows = await query.OrderBy(x => x.Nome)
            .Select(x => new InsumoRow(x.InsumoId, x.CodigoInterno ?? "-", x.Nome, x.UnidadeMedida, x.CustoMedio, x.Ativo))
            .ToListAsync(cancellationToken);
        ViewBag.Query = q;
        return View(rows);
    }

    [NaxapaPermission("INSUMO.EDITAR")]
    [HttpGet]
    public IActionResult Create() => View(new InsumoFormViewModel());

    [NaxapaPermission("INSUMO.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InsumoFormViewModel model, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        await ValidateAsync(model, null, cancellationToken);
        if (!ModelState.IsValid) return View(model);
        db.Insumos.Add(new InsumoEntity
        {
            EstabelecimentoId = est,
            CodigoInterno = Clean(model.CodigoInterno),
            Nome = model.Nome.Trim(),
            UnidadeMedida = model.UnidadeMedida.Trim().ToUpperInvariant(),
            CustoMedio = model.CustoMedio,
            Ativo = model.Ativo,
            CriadoEm = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Insumo cadastrado.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("INSUMO.EDITAR")]
    [HttpGet]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var e = await db.Insumos.AsNoTracking().FirstOrDefaultAsync(x => x.InsumoId == id && x.EstabelecimentoId == currentUser.EstabelecimentoId, cancellationToken);
        if (e is null) return NotFound();
        return View(new InsumoFormViewModel
        {
            InsumoId = e.InsumoId,
            CodigoInterno = e.CodigoInterno,
            Nome = e.Nome,
            UnidadeMedida = e.UnidadeMedida,
            CustoMedio = e.CustoMedio,
            Ativo = e.Ativo
        });
    }

    [NaxapaPermission("INSUMO.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, InsumoFormViewModel model, CancellationToken cancellationToken)
    {
        await ValidateAsync(model, id, cancellationToken);
        if (!ModelState.IsValid) { model.InsumoId = id; return View(model); }
        var e = await db.Insumos.FirstOrDefaultAsync(x => x.InsumoId == id && x.EstabelecimentoId == currentUser.EstabelecimentoId, cancellationToken);
        if (e is null) return NotFound();
        e.CodigoInterno = Clean(model.CodigoInterno); e.Nome = model.Nome.Trim(); e.UnidadeMedida = model.UnidadeMedida.Trim().ToUpperInvariant();
        e.CustoMedio = model.CustoMedio; e.Ativo = model.Ativo; e.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Insumo atualizado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAsync(InsumoFormViewModel model, long? currentId, CancellationToken ct)
    {
        var est = currentUser.EstabelecimentoId;
        var code = Clean(model.CodigoInterno);
        if (code is not null && await db.Insumos.AnyAsync(x => x.EstabelecimentoId == est && x.CodigoInterno == code && x.InsumoId != currentId, ct))
            ModelState.AddModelError(nameof(model.CodigoInterno), "Código já cadastrado.");
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
