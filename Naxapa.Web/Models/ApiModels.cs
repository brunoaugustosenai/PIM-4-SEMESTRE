using System.ComponentModel.DataAnnotations;

namespace Naxapa.Web.Models;

public sealed record ApiPagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total);
public sealed record ApiProductDto(long Id, string? Code, string Name, string Category, decimal Price, string Unit, bool Available, string? ImageUrl);
public sealed record ApiCategoryDto(long Id, string Name, int Order, string? ImageUrl);
public sealed record ApiOptionDto(long Id, string Name, decimal AdditionalValue, int Order);
public sealed record ApiOptionGroupDto(long Id, string Name, string SelectionType, int Minimum, int Maximum, bool Required, IReadOnlyList<ApiOptionDto> Options);
public sealed record ApiProductDetailDto(long Id, string? Code, string Name, string? Description, string Category, decimal Price, string Unit, string? ImageUrl, IReadOnlyList<ApiOptionGroupDto> OptionGroups);

public sealed class ApiClientCreateRequest
{
    [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
    [StringLength(14)] public string? Document { get; set; }
    [StringLength(20)] public string? Phone { get; set; }
    [EmailAddress, StringLength(256)] public string? Email { get; set; }
}

public sealed record ApiClientDto(long Id, string Name, string? Document, string? Phone, string? Email);

public sealed class ApiOrderCreateRequest
{
    [Required] public string ServiceType { get; set; } = "BALCAO";
    public long? ClientId { get; set; }
    public long? TableId { get; set; }
    public long? TabId { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
}

public sealed class ApiOrderItemCreateRequest
{
    [Required] public long ProductId { get; set; }
    [Range(0.001, 999999)] public decimal Quantity { get; set; } = 1;
    [StringLength(500)] public string? Notes { get; set; }
}

public sealed class ApiOrderOptionCreateRequest
{
    [Required] public long OptionId { get; set; }
    [Range(1, 99)] public int Quantity { get; set; } = 1;
}

public sealed class ApiStatusRequest
{
    [Required] public string Status { get; set; } = string.Empty;
    [StringLength(500)] public string? Notes { get; set; }
}

public sealed class ApiPaymentRequest
{
    [Required] public long PaymentMethodId { get; set; }
    [Range(0.01, 999999999)] public decimal Value { get; set; }
    public decimal? ReceivedValue { get; set; }
    [StringLength(150)] public string? TransactionReference { get; set; }
    public long? CashSessionId { get; set; }
}

public sealed record ApiOrderSummaryDto(long Id, long Number, string Status, string FinancialStatus, decimal Total, DateTime CreatedAtUtc);
public sealed record ApiPaymentMethodDto(long Id, string Code, string Name, string Type, bool AllowsChange);

public sealed class ApiCashOpenRequest
{
    [Required] public long CashRegisterId { get; set; }
    [Range(0, 999999999)] public decimal OpeningValue { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
}
public sealed class ApiCashMoveRequest
{
    [Required] public string Type { get; set; } = "SUPRIMENTO";
    [Range(0.01, 999999999)] public decimal Value { get; set; }
    [StringLength(500)] public string? Description { get; set; }
}
public sealed class ApiCashCloseRequest
{
    [Range(0, 999999999)] public decimal CountedValue { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
}


public sealed record ApiTableDto(long Id, int Number, string? Name, int? Capacity, string Status, bool Active);
public sealed class ApiTableCreateRequest
{
    [Range(1, 9999)] public int Number { get; set; }
    [StringLength(100)] public string? Name { get; set; }
    [Range(1, 999)] public int? Capacity { get; set; }
}

public sealed record ApiTabDto(long Id, string Code, string Status, long? ClientId, DateTime? OpenedAtUtc, DateTime? ClosedAtUtc, string? Notes);
public sealed class ApiTabCreateRequest
{
    [Required, StringLength(50)] public string Code { get; set; } = string.Empty;
    public long? ClientId { get; set; }
    [StringLength(300)] public string? Notes { get; set; }
}

public sealed record ApiPrintJobDto(long Id, long? OrderId, long PrinterId, long? ProductionSectorId, string DocumentType, string Status, int Attempts, Guid IdempotencyKey, string PayloadJson, DateTime RequestedAtUtc, string? LastError);
public sealed class ApiPrintFailRequest
{
    [Required, StringLength(1000)] public string Error { get; set; } = string.Empty;
}

public sealed record ApiPrinterDto(long Id, string Name, string? Model, string ConnectionType, string? IpAddress, int? Port, string? WindowsName, int? PaperWidth, bool Default, bool Active);

public sealed record ApiCashRegisterDto(long Id, string Code, string Name, string? Description, bool Active);
