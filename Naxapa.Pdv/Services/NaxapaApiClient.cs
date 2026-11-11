using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Naxapa.Pdv.Models;

namespace Naxapa.Pdv.Services;

public sealed class NaxapaApiClient
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    public string? Token { get; private set; }
    public TokenResponse? Session { get; private set; }
    public string BaseUrl { get; private set; }

    public NaxapaApiClient(string baseUrl) => BaseUrl = Normalize(baseUrl);

    public void SetBaseUrl(string url) => BaseUrl = Normalize(url);

    public async Task<TokenResponse> LoginAsync(string login, string password, long? establishmentId, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/v1/auth/token", new { login, password, estabelecimentoId = establishmentId }, _json, ct);
        await EnsureSuccessAsync(response, ct);
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(_json, ct) ?? throw new InvalidOperationException("Resposta de autenticação inválida.");
        Token = token.Token; Session = token;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        return token;
    }

    public Task<PagedResponse<ProductDto>> ProductsAsync(string? query = null, int page = 1, int pageSize = 100, CancellationToken ct = default)
        => GetAsync<PagedResponse<ProductDto>>($"/api/v1/products?q={Uri.EscapeDataString(query ?? string.Empty)}&page={page}&pageSize={pageSize}", ct);

    public Task<ProductDetailDto> ProductAsync(long id, CancellationToken ct = default)
        => GetAsync<ProductDetailDto>($"/api/v1/products/{id}", ct);

    public Task<PagedResponse<OrderDto>> OrdersAsync(string? status = null, CancellationToken ct = default)
        => GetAsync<PagedResponse<OrderDto>>($"/api/v1/orders?status={Uri.EscapeDataString(status ?? string.Empty)}&page=1&pageSize=100", ct);

    public Task<IReadOnlyList<PaymentMethodDto>> PaymentMethodsAsync(CancellationToken ct = default)
        => GetAsync<IReadOnlyList<PaymentMethodDto>>("/api/v1/payment-methods", ct);

    public Task<IReadOnlyList<TableDto>> TablesAsync(CancellationToken ct = default)
        => GetAsync<IReadOnlyList<TableDto>>("/api/v1/tables", ct);

    public Task<IReadOnlyList<TabDto>> TabsAsync(string? status = null, CancellationToken ct = default)
        => GetAsync<IReadOnlyList<TabDto>>($"/api/v1/tabs?status={Uri.EscapeDataString(status ?? string.Empty)}", ct);

    public Task<IReadOnlyList<CashSummaryDto>> CashAsync(CancellationToken ct = default)
        => GetAsync<IReadOnlyList<CashSummaryDto>>("/api/v1/cash", ct);


    public Task<IReadOnlyList<CashRegisterDto>> CashRegistersAsync(CancellationToken ct = default)
        => GetAsync<IReadOnlyList<CashRegisterDto>>("/api/v1/cash-registers", ct);

    public async Task<TabDto> OpenTabAsync(string code, long? clientId = null, string? notes = null, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/v1/tabs/open", new { code, clientId, notes }, _json, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<TabDto>(_json, ct) ?? throw new InvalidOperationException("Resposta inválida ao abrir comanda.");
    }

    public async Task CloseTabAsync(long id, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"{BaseUrl}/api/v1/tabs/{id}/close", null, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task<OrderDto> CreateOrderAsync(string serviceType, long? clientId = null, long? tableId = null, long? tabId = null, string? notes = null, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/v1/orders", new { serviceType, clientId, tableId, tabId, notes }, _json, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<OrderDto>(_json, ct) ?? throw new InvalidOperationException("Resposta inválida ao criar pedido.");
    }

    public async Task<OrderItemCreatedDto> AddItemAsync(long orderId, long productId, decimal quantity, string? notes = null, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/v1/orders/{orderId}/items", new { productId, quantity, notes }, _json, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<OrderItemCreatedDto>(_json, ct) ?? throw new InvalidOperationException("Resposta inválida ao adicionar item.");
    }

    public async Task AddOptionAsync(long itemId, long optionId, int quantity = 1, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/v1/orders/items/{itemId}/options", new { optionId, quantity }, _json, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task ChangeStatusAsync(long orderId, string status, string? notes = null, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/v1/orders/{orderId}/status", new { status, notes }, _json, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task PayAsync(long orderId, long paymentMethodId, decimal value, decimal? receivedValue, long? cashSessionId, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/v1/orders/{orderId}/payments", new { paymentMethodId, value, receivedValue, cashSessionId }, _json, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task OpenCashAsync(long cashRegisterId, decimal openingValue, string? notes = null, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/v1/cash/open", new { cashRegisterId, openingValue, notes }, _json, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task MoveCashAsync(long sessionId, string type, decimal value, string? description, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/v1/cash/{sessionId}/movements", new { type, value, description }, _json, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task CloseCashAsync(long sessionId, decimal countedValue, string? notes, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/v1/cash/{sessionId}/close", new { countedValue, notes }, _json, ct);
        await EnsureSuccessAsync(response, ct);
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        using var response = await _http.GetAsync(BaseUrl + path, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(_json, ct) ?? throw new InvalidOperationException("Resposta vazia da API.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(ct);
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var detail = root.TryGetProperty("detail", out var d) ? d.GetString() : null;
            var title = root.TryGetProperty("title", out var t) ? t.GetString() : null;
            throw new InvalidOperationException(detail ?? title ?? $"HTTP {(int)response.StatusCode}");
        }
        catch (JsonException)
        {
            throw new InvalidOperationException($"HTTP {(int)response.StatusCode}: {body}");
        }
    }

    private static string Normalize(string url)
    {
        url = url.Trim().TrimEnd('/');
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("Endereço da API inválido.");
        return url;
    }
}
