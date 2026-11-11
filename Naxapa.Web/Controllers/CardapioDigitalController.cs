using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Models;

namespace Naxapa.Web.Controllers;

[AllowAnonymous]
[Route("cardapio-digital")]
public sealed class CardapioDigitalController(NaxapaDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(long? estabelecimentoId = null, CancellationToken cancellationToken = default)
    {
        var establishmentQuery = db.Estabelecimentos.AsNoTracking().Where(x => x.Ativo);
        var establishment = estabelecimentoId is not null
            ? await establishmentQuery.FirstOrDefaultAsync(x => x.EstabelecimentoId == estabelecimentoId.Value, cancellationToken)
            : await establishmentQuery.OrderBy(x => x.EstabelecimentoId).FirstOrDefaultAsync(cancellationToken);

        if (establishment is null)
            return NotFound("Estabelecimento não encontrado.");

        var rows = await (
            from p in db.Produtos.AsNoTracking()
            join c in db.Categorias.AsNoTracking() on p.CategoriaId equals c.CategoriaId
            where p.EstabelecimentoId == establishment.EstabelecimentoId
                  && p.Ativo
                  && p.DisponivelCardapio
                  && c.Ativo
            orderby c.OrdemExibicao, c.Nome, p.Nome
            select new
            {
                p.ProdutoId,
                p.Nome,
                p.Descricao,
                p.PrecoVenda,
                Categoria = c.Nome,
                CategoriaOrdem = c.OrdemExibicao,
                ImagemUrl = db.ProdutoImagens.AsNoTracking()
                    .Where(i => i.ProdutoId == p.ProdutoId && i.Principal)
                    .OrderBy(i => i.OrdemExibicao)
                    .Select(i => i.Url)
                    .FirstOrDefault()
            }).ToListAsync(cancellationToken);

        var categories = rows
            .GroupBy(x => new { x.Categoria, x.CategoriaOrdem })
            .OrderBy(x => x.Key.CategoriaOrdem)
            .ThenBy(x => x.Key.Categoria)
            .Select(group => new DigitalMenuCategoryViewModel
            {
                Nome = group.Key.Categoria,
                Produtos = group.Select(x => new DigitalMenuProductViewModel
                {
                    ProdutoId = x.ProdutoId,
                    Nome = x.Nome,
                    Descricao = x.Descricao,
                    Preco = x.PrecoVenda,
                    ImagemUrl = x.ImagemUrl
                }).ToList()
            }).ToList();

        return View(new DigitalMenuViewModel
        {
            EstabelecimentoId = establishment.EstabelecimentoId,
            Estabelecimento = establishment.NomeFantasia,
            LogoUrl = establishment.LogoUrl,
            Categorias = categories
        });
    }
}
