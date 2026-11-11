using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Entities;
using Naxapa.Web.Models;
using Naxapa.Web.Security;

namespace Naxapa.Web.Controllers;

[Authorize]
public sealed class ClientesController(NaxapaDbContext db, CurrentUserService currentUser) : Controller
{
    [NaxapaPermission("CLIENTE.VISUALIZAR")]
    public async Task<IActionResult> Index(string? q = null, bool? ativo = null, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 10, 100);
        var est = currentUser.EstabelecimentoId;
        var query = db.Clientes.AsNoTracking().Where(x => x.EstabelecimentoId == est);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim();
            query = query.Where(x => x.Nome.Contains(search)
                                     || (x.Documento != null && x.Documento.Contains(search))
                                     || (x.Cidade != null && x.Cidade.Contains(search))
                                     || (x.Bairro != null && x.Bairro.Contains(search)));
        }
        if (ativo is not null) query = query.Where(x => x.Ativo == ativo.Value);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(x => x.Nome).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.ClienteId, x.Nome, x.Documento, x.Telefone, x.Cidade, x.Bairro, x.Ativo })
            .ToListAsync(cancellationToken);

        ViewBag.Query = q; ViewBag.Active = ativo; ViewBag.Page = page; ViewBag.PageSize = pageSize; ViewBag.Total = total;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        return View(rows.Select(x => new ClientRow(x.ClienteId, x.Nome, MaskDocument(x.Documento), x.Telefone ?? "-", x.Cidade ?? "-", x.Bairro ?? "-", x.Ativo)).ToList());
    }

    [NaxapaPermission("CLIENTE.CRIAR")]
    [HttpGet]
    public IActionResult Create() => View(new ClientFormViewModel());

    [NaxapaPermission("CLIENTE.CRIAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClientFormViewModel model, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        Normalize(model); await ValidateClientAsync(model, est, null, cancellationToken);
        if (!ModelState.IsValid) return View(model);
        db.Clientes.Add(new ClienteEntity
        {
            EstabelecimentoId = est, CodigoInterno = Clean(model.CodigoInterno), Nome = model.Nome.Trim(), Documento = DigitsOrNull(model.Documento),
            Telefone = Clean(model.Telefone), Email = Clean(model.Email), Logradouro = Clean(model.Logradouro), Numero = Clean(model.Numero),
            Complemento = Clean(model.Complemento), Bairro = Clean(model.Bairro), Cidade = Clean(model.Cidade), UF = Clean(model.UF)?.ToUpperInvariant(),
            CEP = DigitsOrNull(model.CEP), Observacao = Clean(model.Observacao), Ativo = model.Ativo, CriadoEm = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Cliente cadastrado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("CLIENTE.EDITAR")]
    [HttpGet]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var client = await db.Clientes.AsNoTracking().FirstOrDefaultAsync(x => x.ClienteId == id && x.EstabelecimentoId == currentUser.EstabelecimentoId, cancellationToken);
        if (client is null) return NotFound();
        return View(new ClientFormViewModel
        {
            ClienteId = client.ClienteId, CodigoInterno = client.CodigoInterno, Nome = client.Nome, Documento = client.Documento,
            Telefone = client.Telefone, Email = client.Email, Logradouro = client.Logradouro, Numero = client.Numero,
            Complemento = client.Complemento, Bairro = client.Bairro, Cidade = client.Cidade, UF = client.UF, CEP = client.CEP,
            Observacao = client.Observacao, Ativo = client.Ativo
        });
    }

    [NaxapaPermission("CLIENTE.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, ClientFormViewModel model, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        Normalize(model); await ValidateClientAsync(model, est, id, cancellationToken);
        if (!ModelState.IsValid) { model.ClienteId = id; return View(model); }
        var client = await db.Clientes.FirstOrDefaultAsync(x => x.ClienteId == id && x.EstabelecimentoId == est, cancellationToken);
        if (client is null) return NotFound();
        client.CodigoInterno = Clean(model.CodigoInterno); client.Nome = model.Nome.Trim(); client.Documento = DigitsOrNull(model.Documento);
        client.Telefone = Clean(model.Telefone); client.Email = Clean(model.Email); client.Logradouro = Clean(model.Logradouro); client.Numero = Clean(model.Numero);
        client.Complemento = Clean(model.Complemento); client.Bairro = Clean(model.Bairro); client.Cidade = Clean(model.Cidade); client.UF = Clean(model.UF)?.ToUpperInvariant();
        client.CEP = DigitsOrNull(model.CEP); client.Observacao = Clean(model.Observacao); client.Ativo = model.Ativo; client.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Cliente atualizado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("CLIENTE.EDITAR")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(long id, CancellationToken cancellationToken)
    {
        var client = await db.Clientes.FirstOrDefaultAsync(x => x.ClienteId == id && x.EstabelecimentoId == currentUser.EstabelecimentoId, cancellationToken);
        if (client is null) return NotFound();
        client.Ativo = !client.Ativo; client.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = client.Ativo ? "Cliente ativado." : "Cliente desativado.";
        return RedirectToAction(nameof(Index));
    }

    [NaxapaPermission("CLIENTE.VISUALIZAR")]
    [HttpGet]
    public async Task<IActionResult> History(long id, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var client = await db.Clientes.AsNoTracking().FirstOrDefaultAsync(x => x.ClienteId == id && x.EstabelecimentoId == est, cancellationToken);
        if (client is null) return NotFound();
        var orders = await db.Pedidos.AsNoTracking().Where(x => x.EstabelecimentoId == est && x.ClienteId == id)
            .OrderByDescending(x => x.CriadoEm).Take(100)
            .Select(x => new OrderRow(x.Numero, client.Nome, x.TipoAtendimento, x.StatusOperacional, x.StatusFinanceiro, x.Total, x.CriadoEm))
            .ToListAsync(cancellationToken);
        ViewBag.ClientName = client.Nome;
        return View(orders);
    }

    private async Task ValidateClientAsync(ClientFormViewModel model, long est, long? currentId, CancellationToken ct)
    {
        var code = Clean(model.CodigoInterno);
        if (code is not null && await db.Clientes.AnyAsync(x => x.EstabelecimentoId == est && x.CodigoInterno == code && x.ClienteId != currentId, ct))
            ModelState.AddModelError(nameof(model.CodigoInterno), "Já existe um cliente com este código interno.");
        var doc = DigitsOrNull(model.Documento);
        if (doc is not null && doc.Length is not (11 or 14)) ModelState.AddModelError(nameof(model.Documento), "CPF deve ter 11 dígitos e CNPJ 14 dígitos.");
        if (doc is not null && await db.Clientes.AnyAsync(x => x.EstabelecimentoId == est && x.Documento == doc && x.ClienteId != currentId, ct))
            ModelState.AddModelError(nameof(model.Documento), "Já existe um cliente com este CPF/CNPJ.");
        var cep = DigitsOrNull(model.CEP);
        if (cep is not null && cep.Length != 8) ModelState.AddModelError(nameof(model.CEP), "CEP deve possuir 8 dígitos.");
    }
    private static void Normalize(ClientFormViewModel m) { m.Documento = DigitsOrNull(m.Documento); m.CEP = DigitsOrNull(m.CEP); m.UF = Clean(m.UF)?.ToUpperInvariant(); }
    private static string? DigitsOrNull(string? value) { if (string.IsNullOrWhiteSpace(value)) return null; var d = Regex.Replace(value, "[^0-9]", string.Empty); return string.IsNullOrEmpty(d) ? null : d; }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string MaskDocument(string? document) => string.IsNullOrWhiteSpace(document) ? "-" : document.Length switch
    {
        11 => $"{document[..3]}.***.***-{document[^2..]}",
        14 => $"{document[..2]}.***.***/****-{document[^2..]}",
        _ => document
    };
}
