using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Naxapa.Web.Models;

public sealed class EstablishmentFormViewModel
{
    public long? EstabelecimentoId { get; set; }

    [StringLength(30)]
    [Display(Name = "Código")]
    public string? Codigo { get; set; }

    [Required, StringLength(150)]
    [Display(Name = "Nome fantasia")]
    public string NomeFantasia { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Razão social")]
    public string? RazaoSocial { get; set; }

    [StringLength(18)]
    [Display(Name = "CPF/CNPJ")]
    public string? Documento { get; set; }

    [StringLength(20)]
    [Display(Name = "Inscrição estadual")]
    public string? InscricaoEstadual { get; set; }

    [StringLength(20)] public string? Telefone { get; set; }
    [EmailAddress, StringLength(256)] public string? Email { get; set; }
    [StringLength(150)] public string? Logradouro { get; set; }
    [StringLength(20)] public string? Numero { get; set; }
    [StringLength(100)] public string? Complemento { get; set; }
    [StringLength(100)] public string? Bairro { get; set; }
    [StringLength(100)] public string? Cidade { get; set; }
    [StringLength(2, MinimumLength = 2)] public string? UF { get; set; }
    [StringLength(9)] public string? CEP { get; set; }

    [Display(Name = "Horário de abertura")]
    public TimeSpan? HoraAbertura { get; set; }

    [Display(Name = "Horário de fechamento")]
    public TimeSpan? HoraFechamento { get; set; }

    public bool Ativo { get; set; } = true;
}

public sealed record EstablishmentRow(long Id, string Codigo, string Nome, string Documento, string Cidade, string UF, bool Ativo);

public sealed class CategoryFormViewModel
{
    public long? CategoriaId { get; set; }
    [Required, StringLength(100)] public string Nome { get; set; } = string.Empty;
    [StringLength(300)] public string? Descricao { get; set; }
    [Range(0, int.MaxValue)] [Display(Name = "Ordem de exibição")] public int OrdemExibicao { get; set; }
    [Display(Name = "Imagem")] public IFormFile? Imagem { get; set; }
    public string? ImagemAtual { get; set; }
    public bool Ativo { get; set; } = true;
}

public sealed record CategoryRow(long Id, string Nome, int Ordem, int Produtos, bool Ativo, string? ImagemUrl);

public sealed class SectorFormViewModel
{
    public long? SetorProducaoId { get; set; }
    [Required, StringLength(100)] public string Nome { get; set; } = string.Empty;
    [StringLength(300)] public string? Descricao { get; set; }
    public bool Ativo { get; set; } = true;
}

public sealed record SectorRow(long Id, string Nome, string Descricao, int Produtos, bool Ativo);

public sealed class PrinterFormViewModel
{
    public long? ImpressoraId { get; set; }
    [Required, StringLength(120)] public string Nome { get; set; } = string.Empty;
    [StringLength(120)] public string? Modelo { get; set; }
    [Required] [Display(Name = "Tipo de conexão")] public string TipoConexao { get; set; } = "WINDOWS";
    [StringLength(45)] [Display(Name = "Endereço IP")] public string? EnderecoIP { get; set; }
    [Range(1, 65535)] public int? Porta { get; set; }
    [StringLength(200)] [Display(Name = "Impressora no Windows")] public string? NomeWindows { get; set; }
    [Range(40, 120)] [Display(Name = "Largura do papel (mm)")] public int? LarguraPapel { get; set; } = 80;
    [Display(Name = "Setor")] public long? SetorProducaoId { get; set; }
    [Display(Name = "Impressora padrão")] public bool Padrao { get; set; }
    public bool Ativo { get; set; } = true;
    public IReadOnlyList<SelectListItem> Setores { get; set; } = [];
}

public sealed record PrinterRow(long Id, string Nome, string Modelo, string Conexao, string Destino, string Setor, bool Padrao, bool Ativo);

public sealed class MenuFormViewModel
{
    public long? CardapioId { get; set; }
    [Required, StringLength(120)] public string Nome { get; set; } = string.Empty;
    [StringLength(300)] public string? Descricao { get; set; }
    [Display(Name = "Início")] public DateTime? DataInicio { get; set; }
    [Display(Name = "Fim")] public DateTime? DataFim { get; set; }
    public bool Publicado { get; set; }
    public bool Ativo { get; set; } = true;
}

public sealed record MenuRow(long Id, string Nome, int Produtos, bool Publicado, bool Ativo, DateTime? Inicio, DateTime? Fim);
public sealed record MenuProductChoice(long ProdutoId, string Nome, string Categoria, decimal Preco, bool Selecionado, bool Disponivel, int Ordem);

public sealed class MenuProductsViewModel
{
    public long CardapioId { get; set; }
    public string Cardapio { get; set; } = string.Empty;
    public List<long> ProdutoIds { get; set; } = [];
    public IReadOnlyList<MenuProductChoice> Produtos { get; set; } = [];
}

public sealed class OptionGroupFormViewModel
{
    public long? GrupoOpcaoId { get; set; }
    [Required, StringLength(120)] public string Nome { get; set; } = string.Empty;
    [StringLength(300)] public string? Descricao { get; set; }
    [Required] [Display(Name = "Tipo de seleção")] public string TipoSelecao { get; set; } = "U";
    [Range(0, 99)] [Display(Name = "Quantidade mínima")] public int QuantidadeMinima { get; set; }
    [Range(1, 99)] [Display(Name = "Quantidade máxima")] public int QuantidadeMaxima { get; set; } = 1;
    public bool Obrigatorio { get; set; }
    public bool Ativo { get; set; } = true;
}

public sealed class OptionFormViewModel
{
    public long? OpcaoProdutoId { get; set; }
    [Required] public long GrupoOpcaoId { get; set; }
    [Required, StringLength(120)] public string Nome { get; set; } = string.Empty;
    [Range(0, 999999999)] [Display(Name = "Valor adicional")] public decimal ValorAdicional { get; set; }
    [Range(0, int.MaxValue)] public int OrdemExibicao { get; set; }
    public bool Ativo { get; set; } = true;
}

public sealed record OptionGroupRow(long Id, string Nome, string Tipo, int Minimo, int Maximo, bool Obrigatorio, int Opcoes, bool Ativo);
public sealed record OptionRow(long Id, long GrupoId, string Grupo, string Nome, decimal Valor, int Ordem, bool Ativo);

public sealed class InsumoFormViewModel
{
    public long? InsumoId { get; set; }
    [StringLength(30)] [Display(Name = "Código") ] public string? CodigoInterno { get; set; }
    [Required, StringLength(150)] public string Nome { get; set; } = string.Empty;
    [Required, StringLength(10)] [Display(Name = "Unidade")] public string UnidadeMedida { get; set; } = "UN";
    [Range(0, 999999999)] [Display(Name = "Custo médio")] public decimal? CustoMedio { get; set; }
    public bool Ativo { get; set; } = true;
}

public sealed record InsumoRow(long Id, string Codigo, string Nome, string Unidade, decimal? CustoMedio, bool Ativo);

public sealed class RecipeFormViewModel
{
    public long? ReceitaId { get; set; }
    [Required] [Display(Name = "Produto")] public long ProdutoId { get; set; }
    [Range(0.001, 999999)] public decimal Rendimento { get; set; } = 1;
    [StringLength(500)] public string? Observacao { get; set; }
    public bool Ativo { get; set; } = true;
    public IReadOnlyList<SelectListItem> Produtos { get; set; } = [];
}

public sealed class RecipeItemFormViewModel
{
    public long ReceitaId { get; set; }
    [Required] public long InsumoId { get; set; }
    [Range(0.0001, 999999)] public decimal Quantidade { get; set; }
    [StringLength(300)] public string? Observacao { get; set; }
    public IReadOnlyList<SelectListItem> Insumos { get; set; } = [];
}

public sealed record RecipeRow(long Id, long ProdutoId, string Produto, decimal Rendimento, int Itens, bool Ativo);
public sealed record RecipeItemRow(long Id, string Insumo, decimal Quantidade, string Unidade, decimal? CustoUnitario, decimal? CustoItem);

public sealed class CashRegisterFormViewModel
{
    public long? CaixaId { get; set; }
    [Required, StringLength(30)] public string Codigo { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Nome { get; set; } = string.Empty;
    [StringLength(300)] public string? Descricao { get; set; }
    public bool Ativo { get; set; } = true;
}

public sealed class CashOpenViewModel
{
    [Required] public long CaixaId { get; set; }
    [Range(0, 999999999)] [Display(Name = "Valor inicial")] public decimal ValorAbertura { get; set; }
    [StringLength(500)] public string? Observacao { get; set; }
    public IReadOnlyList<SelectListItem> Caixas { get; set; } = [];
}

public sealed class CashMovementViewModel
{
    public long CaixaSessaoId { get; set; }
    [Required] public string TipoMovimento { get; set; } = "SUPRIMENTO";
    [Range(0.01, 999999999)] public decimal Valor { get; set; }
    [StringLength(500)] public string? Descricao { get; set; }
}

public sealed class CashCloseViewModel
{
    public long CaixaSessaoId { get; set; }
    [Range(0, 999999999)] [Display(Name = "Valor contado")] public decimal ValorFechamentoInformado { get; set; }
    [StringLength(500)] public string? Observacao { get; set; }
}

public sealed class DashboardFilterViewModel
{
    public string Periodo { get; set; } = "hoje";
    public DateTime? DataInicial { get; set; }
    public DateTime? DataFinal { get; set; }
}

public sealed record ChartPoint(string Label, decimal Value);
public sealed record PaymentMethodMetric(string Forma, decimal Valor, long Quantidade);

public sealed class ProductImportViewModel
{
    [Required] [Display(Name = "Arquivo CSV")] public IFormFile? Arquivo { get; set; }
    public string? ImportToken { get; set; }
    public IReadOnlyList<ProductImportRow> Preview { get; set; } = [];
}

public sealed record ProductImportRow(int Linha, string Codigo, string Nome, string Categoria, decimal? Preco, string Setor, bool Valido, string Erro);

public sealed class OptionGroupProductsViewModel
{
    public long GrupoOpcaoId { get; set; }
    public string Grupo { get; set; } = string.Empty;
    public List<long> ProdutoIds { get; set; } = [];
    public IReadOnlyList<SelectListItem> Produtos { get; set; } = [];
}

public sealed record PrinterJobRow(long Id,string Printer,string DocumentType,string Status,int Attempts,DateTime RequestedAt,DateTime? ProcessedAt,string? LastError,long? OrderId);
