using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;

namespace Naxapa.Web.Models;

public sealed record DashboardMetric(string Label, string Value, string Hint, string Icon);

public sealed record ProductRow(
    long Id,
    string Codigo,
    string Nome,
    string Categoria,
    decimal Preco,
    string Setor,
    bool Disponivel,
    bool Ativo);

public sealed record ClientRow(
    long Id,
    string Nome,
    string Documento,
    string Telefone,
    string Cidade,
    string Bairro,
    bool Ativo);

public sealed record OrderRow(
    long Numero,
    string Cliente,
    string Tipo,
    string Status,
    string Financeiro,
    decimal Total,
    DateTime CriadoEm);

public sealed record CashSessionRow(
    long Id,
    string Caixa,
    string Operador,
    string Status,
    DateTime AbertoEm,
    decimal Abertura,
    decimal Vendas,
    decimal Sangrias,
    decimal Suprimentos,
    decimal SaldoEsperado,
    decimal? Diferenca);

public sealed record MenuCategory(string Nome, int Produtos, bool Ativa);

public sealed class DashboardViewModel
{
    public IReadOnlyList<DashboardMetric> Metrics { get; init; } = [];
    public IReadOnlyList<OrderRow> RecentOrders { get; init; } = [];
    public IReadOnlyList<ProductRow> TopProducts { get; init; } = [];
    public IReadOnlyList<ChartPoint> RevenueChart { get; init; } = [];
    public IReadOnlyList<PaymentMethodMetric> PaymentMethods { get; init; } = [];
    public IReadOnlyList<OperatorMetric> Operators { get; init; } = [];
    public IReadOnlyList<CategorySalesMetric> Categories { get; init; } = [];
    public string Estabelecimento { get; init; } = "NAXAPA";
    public string PeriodoLabel { get; init; } = string.Empty;
    public string Periodo { get; init; } = "hoje";
    public DateTime DataInicial { get; init; }
    public DateTime DataFinal { get; init; }
}

public sealed class ProductFormViewModel
{
    public long? ProdutoId { get; set; }

    [Display(Name = "Código interno")]
    [StringLength(30)]
    public string? CodigoInterno { get; set; }

    [Display(Name = "GTIN-13 / Código de barras")]
    [StringLength(30)]
    public string? CodigoBarras { get; set; }

    [Required(ErrorMessage = "Informe o nome do produto.")]
    [StringLength(150)]
    public string Nome { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Descricao { get; set; }

    [Required(ErrorMessage = "Selecione a categoria.")]
    [Range(1, long.MaxValue, ErrorMessage = "Selecione uma categoria válida.")]
    [Display(Name = "Categoria")]
    public long CategoriaId { get; set; }

    [Display(Name = "Setor de produção")]
    public long? SetorProducaoId { get; set; }

    [Required(ErrorMessage = "Informe o preço de venda.")]
    [Range(0, 999999999, ErrorMessage = "Informe um preço válido.")]
    [Display(Name = "Preço de venda")]
    public decimal PrecoVenda { get; set; }

    [Range(0, 999999999, ErrorMessage = "Informe um custo válido.")]
    public decimal? Custo { get; set; }

    [Required]
    [StringLength(10)]
    [Display(Name = "Unidade")]
    public string UnidadeMedida { get; set; } = "UN";

    [Display(Name = "Disponível no cardápio")]
    public bool DisponivelCardapio { get; set; } = true;

    [Display(Name = "Controla estoque")]
    public bool ControlaEstoque { get; set; }

    public bool Ativo { get; set; } = true;

    [Display(Name = "Imagem do produto")]
    public IFormFile? Imagem { get; set; }

    public string? ImagemAtual { get; set; }

    public IReadOnlyList<SelectListItem> Categorias { get; set; } = [];
    public IReadOnlyList<SelectListItem> Setores { get; set; } = [];
}

public sealed class DigitalMenuViewModel
{
    public long EstabelecimentoId { get; init; }
    public string Estabelecimento { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
    public IReadOnlyList<DigitalMenuCategoryViewModel> Categorias { get; init; } = [];
}

public sealed class DigitalMenuCategoryViewModel
{
    public string Nome { get; init; } = string.Empty;
    public IReadOnlyList<DigitalMenuProductViewModel> Produtos { get; init; } = [];
}

public sealed class DigitalMenuProductViewModel
{
    public long ProdutoId { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string? Descricao { get; init; }
    public decimal Preco { get; init; }
    public string? ImagemUrl { get; init; }
}

public sealed class ClientFormViewModel
{
    public long? ClienteId { get; set; }

    [Display(Name = "Código interno")]
    [StringLength(30)]
    public string? CodigoInterno { get; set; }

    [Required(ErrorMessage = "Informe o nome do cliente.")]
    [StringLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Display(Name = "CPF/CNPJ")]
    [StringLength(18)]
    public string? Documento { get; set; }

    [StringLength(20)]
    public string? Telefone { get; set; }

    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(256)]
    public string? Email { get; set; }

    [StringLength(150)]
    public string? Logradouro { get; set; }

    [StringLength(20)]
    public string? Numero { get; set; }

    [StringLength(100)]
    public string? Complemento { get; set; }

    [StringLength(100)]
    public string? Bairro { get; set; }

    [StringLength(100)]
    public string? Cidade { get; set; }

    [StringLength(2, MinimumLength = 2, ErrorMessage = "UF deve possuir 2 caracteres.")]
    public string? UF { get; set; }

    [StringLength(9)]
    public string? CEP { get; set; }

    [StringLength(500)]
    public string? Observacao { get; set; }

    public bool Ativo { get; set; } = true;
}

public sealed class SettingsViewModel
{
    public bool DatabaseReady { get; init; }
    public string DatabaseStatus { get; init; } = string.Empty;
    public string Server { get; init; } = string.Empty;
    public string Database { get; init; } = string.Empty;
    public string Estabelecimento { get; init; } = string.Empty;
}

public sealed record OrderListRow(
    long PedidoId,
    long Numero,
    string Cliente,
    string Tipo,
    string Status,
    string Financeiro,
    decimal Total,
    DateTime CriadoEm,
    string Operador);

public sealed record OrderOptionRow(string Nome, int Quantidade, decimal ValorUnitario, decimal Total);

public sealed class OrderItemDetailViewModel
{
    public long PedidoItemId { get; init; }
    public string Produto { get; init; } = string.Empty;
    public decimal Quantidade { get; init; }
    public decimal ValorUnitario { get; init; }
    public decimal Desconto { get; init; }
    public decimal Acrescimo { get; init; }
    public decimal ValorOpcoes { get; init; }
    public decimal Total { get; init; }
    public string? Observacao { get; init; }
    public IReadOnlyList<OrderOptionRow> Opcoes { get; init; } = [];
}

public sealed record OrderHistoryRow(string? Anterior, string Novo, string Usuario, DateTime CriadoEm, string? Observacao);

public sealed class OrderDetailViewModel
{
    public long PedidoId { get; init; }
    public long Numero { get; init; }
    public string Cliente { get; init; } = string.Empty;
    public string TipoAtendimento { get; init; } = string.Empty;
    public string StatusOperacional { get; init; } = string.Empty;
    public string StatusFinanceiro { get; init; } = string.Empty;
    public decimal Subtotal { get; init; }
    public decimal Desconto { get; init; }
    public decimal Acrescimo { get; init; }
    public decimal Total { get; init; }
    public string Operador { get; init; } = string.Empty;
    public DateTime CriadoEm { get; init; }
    public string? Observacao { get; init; }
    public IReadOnlyList<OrderItemDetailViewModel> Itens { get; init; } = [];
    public IReadOnlyList<OrderHistoryRow> Historico { get; init; } = [];
}

public sealed class OrderIndexViewModel
{
    public IReadOnlyList<OrderListRow> Pedidos { get; init; } = [];
    public string? Status { get; init; }
    public string? Pesquisa { get; init; }
    public DateTime? DataInicial { get; init; }
    public DateTime? DataFinal { get; init; }
    public int Pagina { get; init; } = 1;
    public int TotalPaginas { get; init; } = 1;
    public long TotalRegistros { get; init; }
}

public sealed class CashIndexViewModel
{
    public IReadOnlyList<CashSessionRow> Sessoes { get; init; } = [];
    public IReadOnlyList<CashRegisterRow> Caixas { get; init; } = [];
    public long? SessaoAbertaId { get; init; }
}

public sealed record CashRegisterRow(long Id, string Codigo, string Nome, string Descricao, bool Ativo, bool Aberto);

public sealed record OperatorMetric(string Operador, decimal Valor, long Pedidos);
public sealed record CategorySalesMetric(string Categoria, decimal Valor, decimal Quantidade);
