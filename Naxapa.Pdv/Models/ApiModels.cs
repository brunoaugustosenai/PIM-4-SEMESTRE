namespace Naxapa.Pdv.Models;

public sealed record TokenResponse(string Token, string TokenType, DateTime ExpiresAtUtc, long UserId, string UserName, long EstabelecimentoId, string Estabelecimento);
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total);
public sealed record ProductDto(long Id, string? Code, string Name, string Category, decimal Price, string Unit, bool Available, string? ImageUrl);
public sealed record ProductOptionDto(long Id, string Name, decimal AdditionalValue, int Order);
public sealed record ProductOptionGroupDto(long Id, string Name, string SelectionType, int Minimum, int Maximum, bool Required, IReadOnlyList<ProductOptionDto> Options);
public sealed record ProductDetailDto(long Id, string? Code, string Name, string? Description, string Category, decimal Price, string Unit, string? ImageUrl, IReadOnlyList<ProductOptionGroupDto> OptionGroups);
public sealed record OrderItemCreatedDto(long PedidoItemId, long ProdutoId, string NomeProduto, decimal Quantidade, decimal ValorUnitario, decimal TotalItem);
public sealed record OrderDto(long Id, long Number, string Status, string FinancialStatus, decimal Total, DateTime CreatedAtUtc);
public sealed record PaymentMethodDto(long Id, string Code, string Name, string Type, bool AllowsChange);
public sealed record TableDto(long Id, int Number, string? Name, int? Capacity, string Status, bool Active);
public sealed record TabDto(long Id, string Code, string Status, long? ClientId, DateTime? OpenedAtUtc, DateTime? ClosedAtUtc, string? Notes);
public sealed class CashSummaryDto
{
    public long CaixaSessaoId { get; set; }
    public long CaixaId { get; set; }
    public string Caixa { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal ValorAbertura { get; set; }
    public decimal SaldoFisicoEsperado { get; set; }
    public DateTime AbertoEm { get; set; }
}
public sealed record CashRegisterDto(long Id, string Code, string Name, string? Description, bool Active);

public sealed class CartOptionLine
{
    public long OptionId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal AdditionalValue { get; init; }
    public decimal Total => Quantity * AdditionalValue;
}

public sealed class CartLine
{
    public ProductDto Product { get; init; } = default!;
    public decimal Quantity { get; set; } = 1;
    public List<CartOptionLine> Options { get; init; } = [];
    public string OptionsText => Options.Count == 0 ? string.Empty : string.Join(", ", Options.Select(x => x.Quantity > 1 ? $"{x.Quantity}x {x.Name}" : x.Name));
    public decimal UnitWithOptions => Product.Price + Options.Sum(x => x.Total);
    public decimal Total => Quantity * UnitWithOptions;
}
