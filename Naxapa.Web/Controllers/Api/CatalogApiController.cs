using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Models;
using Naxapa.Web.Security;

namespace Naxapa.Web.Controllers.Api;

[ApiController]
[Route("api/v1")]
[Authorize(AuthenticationSchemes = NaxapaAuthenticationSchemes.ApiBearer)]
[EnableRateLimiting("api")]
public sealed class CatalogApiController(NaxapaDbContext db, CurrentUserService currentUser) : ControllerBase
{
    [HttpGet("products")]
    [NaxapaPermission("PRODUTO.VISUALIZAR")]
    public async Task<ActionResult<ApiPagedResponse<ApiProductDto>>> Products(string? q = null, int page = 1, int pageSize = 30, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100); var est = currentUser.EstabelecimentoId;
        var query = from p in db.Produtos.AsNoTracking()
                    join c in db.Categorias.AsNoTracking() on p.CategoriaId equals c.CategoriaId
                    where p.EstabelecimentoId == est && p.Ativo
                    select new { p, Category = c.Nome };
        if (!string.IsNullOrWhiteSpace(q)) { var term=q.Trim(); query=query.Where(x=>x.p.Nome.Contains(term)||(x.p.CodigoInterno!=null&&x.p.CodigoInterno.Contains(term))); }
        var total=await query.LongCountAsync(cancellationToken);
        var rows=await query.OrderBy(x=>x.p.Nome).Skip((page-1)*pageSize).Take(pageSize).Select(x=>new {x.p.ProdutoId,x.p.CodigoInterno,x.p.Nome,x.Category,x.p.PrecoVenda,x.p.UnidadeMedida,x.p.DisponivelCardapio}).ToListAsync(cancellationToken);
        var ids=rows.Select(x=>x.ProdutoId).ToArray();
        var images=await db.ProdutoImagens.AsNoTracking().Where(x=>ids.Contains(x.ProdutoId)&&x.Principal).ToDictionaryAsync(x=>x.ProdutoId,x=>x.Url,cancellationToken);
        return Ok(new ApiPagedResponse<ApiProductDto>(rows.Select(x=>new ApiProductDto(x.ProdutoId,x.CodigoInterno,x.Nome,x.Category,x.PrecoVenda,x.UnidadeMedida,x.DisponivelCardapio,images.GetValueOrDefault(x.ProdutoId))).ToList(),page,pageSize,total));
    }

    [HttpGet("products/{id:long}")]
    [NaxapaPermission("PRODUTO.VISUALIZAR")]
    public async Task<ActionResult<ApiProductDetailDto>> Product(long id, CancellationToken cancellationToken)
    {
        var est=currentUser.EstabelecimentoId;
        var product=await (from p in db.Produtos.AsNoTracking() join c in db.Categorias.AsNoTracking() on p.CategoriaId equals c.CategoriaId where p.ProdutoId==id&&p.EstabelecimentoId==est&&p.Ativo select new {p,c.Nome}).FirstOrDefaultAsync(cancellationToken);
        if(product is null)return NotFound();
        var image=await db.ProdutoImagens.AsNoTracking().Where(x=>x.ProdutoId==id&&x.Principal).Select(x=>x.Url).FirstOrDefaultAsync(cancellationToken);
        var groupIds=await db.ProdutoGruposOpcao.AsNoTracking().Where(x=>x.ProdutoId==id&&x.EstabelecimentoId==est).OrderBy(x=>x.OrdemExibicao).Select(x=>x.GrupoOpcaoId).ToListAsync(cancellationToken);
        var groups=await db.GruposOpcao.AsNoTracking().Where(x=>x.EstabelecimentoId==est&&groupIds.Contains(x.GrupoOpcaoId)&&x.Ativo).ToListAsync(cancellationToken);
        var options=await db.OpcoesProduto.AsNoTracking().Where(x=>x.EstabelecimentoId==est&&groupIds.Contains(x.GrupoOpcaoId)&&x.Ativo).OrderBy(x=>x.OrdemExibicao).ToListAsync(cancellationToken);
        var dtoGroups=groups.OrderBy(x=>groupIds.IndexOf(x.GrupoOpcaoId)).Select(g=>new ApiOptionGroupDto(g.GrupoOpcaoId,g.Nome,g.TipoSelecao,g.QuantidadeMinima,g.QuantidadeMaxima,g.Obrigatorio,options.Where(o=>o.GrupoOpcaoId==g.GrupoOpcaoId).Select(o=>new ApiOptionDto(o.OpcaoProdutoId,o.Nome,o.ValorAdicional,o.OrdemExibicao)).ToList())).ToList();
        return Ok(new ApiProductDetailDto(product.p.ProdutoId,product.p.CodigoInterno,product.p.Nome,product.p.Descricao,product.Nome,product.p.PrecoVenda,product.p.UnidadeMedida,image,dtoGroups));
    }

    [HttpGet("categories")]
    [NaxapaPermission("CATEGORIA.VISUALIZAR")]
    public async Task<ActionResult<IReadOnlyList<ApiCategoryDto>>> Categories(CancellationToken cancellationToken)
    {
        var est=currentUser.EstabelecimentoId;
        return Ok(await db.Categorias.AsNoTracking().Where(x=>x.EstabelecimentoId==est&&x.Ativo).OrderBy(x=>x.OrdemExibicao).ThenBy(x=>x.Nome).Select(x=>new ApiCategoryDto(x.CategoriaId,x.Nome,x.OrdemExibicao,x.ImagemUrl)).ToListAsync(cancellationToken));
    }

    [HttpGet("menu")]
    [NaxapaPermission("CARDAPIO.VISUALIZAR")]
    public async Task<IActionResult> Menu(CancellationToken cancellationToken)
    {
        var est=currentUser.EstabelecimentoId;var now=DateTime.UtcNow;
        var menu=await db.Cardapios.AsNoTracking().Where(x=>x.EstabelecimentoId==est&&x.Ativo&&x.Publicado&&(x.DataInicio==null||x.DataInicio<=now)&&(x.DataFim==null||x.DataFim>=now)).OrderBy(x=>x.CardapioId).FirstOrDefaultAsync(cancellationToken);
        if(menu is null)return NotFound(new ProblemDetails{Title="Nenhum cardápio publicado"});
        var ids=await db.CardapioProdutos.AsNoTracking().Where(x=>x.CardapioId==menu.CardapioId&&x.EstabelecimentoId==est&&x.Disponivel).OrderBy(x=>x.OrdemExibicao).Select(x=>x.ProdutoId).ToListAsync(cancellationToken);
        var products=await (from p in db.Produtos.AsNoTracking() join c in db.Categorias.AsNoTracking() on p.CategoriaId equals c.CategoriaId where ids.Contains(p.ProdutoId)&&p.EstabelecimentoId==est&&p.Ativo&&p.DisponivelCardapio select new {p,c.Nome}).ToListAsync(cancellationToken);
        return Ok(new { menu = new { menu.CardapioId, menu.Nome, menu.Descricao }, products = products.OrderBy(x=>ids.IndexOf(x.p.ProdutoId)).Select(x=>new ApiProductDto(x.p.ProdutoId,x.p.CodigoInterno,x.p.Nome,x.Nome,x.p.PrecoVenda,x.p.UnidadeMedida,x.p.DisponivelCardapio,null)) });
    }
}
