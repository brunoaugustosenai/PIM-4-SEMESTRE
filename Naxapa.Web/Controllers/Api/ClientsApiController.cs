using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Entities;
using Naxapa.Web.Models;
using Naxapa.Web.Security;

namespace Naxapa.Web.Controllers.Api;

[ApiController]
[Route("api/v1/clients")]
[Authorize(AuthenticationSchemes = NaxapaAuthenticationSchemes.ApiBearer)]
[EnableRateLimiting("api")]
public sealed class ClientsApiController(NaxapaDbContext db, CurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    [NaxapaPermission("CLIENTE.VISUALIZAR")]
    public async Task<ActionResult<ApiPagedResponse<ApiClientDto>>> Get(string? q = null, int page = 1, int pageSize = 30, CancellationToken cancellationToken = default)
    {
        page=Math.Max(1,page);pageSize=Math.Clamp(pageSize,1,100);var est=currentUser.EstabelecimentoId;
        var query=db.Clientes.AsNoTracking().Where(x=>x.EstabelecimentoId==est&&x.Ativo);
        if(!string.IsNullOrWhiteSpace(q)){var term=q.Trim();query=query.Where(x=>x.Nome.Contains(term)||(x.Documento!=null&&x.Documento.Contains(term)));}
        var total=await query.LongCountAsync(cancellationToken);
        var items=await query.OrderBy(x=>x.Nome).Skip((page-1)*pageSize).Take(pageSize).Select(x=>new ApiClientDto(x.ClienteId,x.Nome,x.Documento,x.Telefone,x.Email)).ToListAsync(cancellationToken);
        return Ok(new ApiPagedResponse<ApiClientDto>(items,page,pageSize,total));
    }

    [HttpGet("{id:long}")]
    [NaxapaPermission("CLIENTE.VISUALIZAR")]
    public async Task<ActionResult<ApiClientDto>> GetById(long id, CancellationToken cancellationToken)
    {
        var est = currentUser.EstabelecimentoId;
        var item = await db.Clientes.AsNoTracking()
            .Where(x => x.EstabelecimentoId == est && x.ClienteId == id)
            .Select(x => new ApiClientDto(x.ClienteId, x.Nome, x.Documento, x.Telefone, x.Email))
            .FirstOrDefaultAsync(cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [NaxapaPermission("CLIENTE.CRIAR")]
    public async Task<ActionResult<ApiClientDto>> Post(ApiClientCreateRequest request, CancellationToken cancellationToken)
    {
        var est=currentUser.EstabelecimentoId;
        var doc=string.IsNullOrWhiteSpace(request.Document)?null:new string(request.Document.Where(char.IsDigit).ToArray());
        if(doc is not null && doc.Length is not (11 or 14)) ModelState.AddModelError(nameof(request.Document),"CPF/CNPJ inválido.");
        if(doc is not null && await db.Clientes.AsNoTracking().AnyAsync(x=>x.EstabelecimentoId==est&&x.Documento==doc,cancellationToken)) ModelState.AddModelError(nameof(request.Document),"Documento já cadastrado.");
        if(!ModelState.IsValid)return ValidationProblem(ModelState);
        var entity=new ClienteEntity{EstabelecimentoId=est,Nome=request.Name.Trim(),Documento=doc,Telefone=Clean(request.Phone),Email=Clean(request.Email),Ativo=true,CriadoEm=DateTime.UtcNow};
        db.Clientes.Add(entity);await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById),new{id=entity.ClienteId},new ApiClientDto(entity.ClienteId,entity.Nome,entity.Documento,entity.Telefone,entity.Email));
    }
    private static string? Clean(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
}
