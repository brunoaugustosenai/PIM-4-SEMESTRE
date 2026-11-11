using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Naxapa.PrintAgent.Models;
namespace Naxapa.PrintAgent.Services;
public sealed class ApiClient(string baseUrl)
{
    private readonly HttpClient _http=new(){Timeout=TimeSpan.FromSeconds(20)};
    private readonly JsonSerializerOptions _json=new(JsonSerializerDefaults.Web){PropertyNameCaseInsensitive=true};
    private string BaseUrl { get; }=baseUrl.Trim().TrimEnd('/');
    public async Task<TokenResponse> LoginAsync(string login,string password,long? establishmentId,CancellationToken ct){var r=await _http.PostAsJsonAsync($"{BaseUrl}/api/v1/auth/token",new{login,password,estabelecimentoId=establishmentId},_json,ct);await Ensure(r,ct);var token=await r.Content.ReadFromJsonAsync<TokenResponse>(_json,ct)??throw new InvalidOperationException("Resposta de login inválida.");_http.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token.Token);return token;}
    public async Task<IReadOnlyList<PrinterDto>> PrintersAsync(CancellationToken ct){var r=await _http.GetAsync($"{BaseUrl}/api/v1/print-jobs/printers",ct);await Ensure(r,ct);return await r.Content.ReadFromJsonAsync<List<PrinterDto>>(_json,ct)??[];}
    public async Task<IReadOnlyList<PrintJobDto>> PendingAsync(CancellationToken ct){var r=await _http.GetAsync($"{BaseUrl}/api/v1/print-jobs/pending?limit=20",ct);await Ensure(r,ct);return await r.Content.ReadFromJsonAsync<List<PrintJobDto>>(_json,ct)??[];}
    public Task ClaimAsync(long id,CancellationToken ct)=>PostEmpty($"/api/v1/print-jobs/{id}/claim",null,ct);
    public Task CompleteAsync(long id,CancellationToken ct)=>PostEmpty($"/api/v1/print-jobs/{id}/complete",null,ct);
    public Task FailAsync(long id,string error,CancellationToken ct)=>PostEmpty($"/api/v1/print-jobs/{id}/fail",new{error},ct);
    private async Task PostEmpty(string path,object? body,CancellationToken ct){var r=body is null?await _http.PostAsync(BaseUrl+path,null,ct):await _http.PostAsJsonAsync(BaseUrl+path,body,_json,ct);await Ensure(r,ct);}
    private static async Task Ensure(HttpResponseMessage r,CancellationToken ct){if(r.IsSuccessStatusCode)return;var body=await r.Content.ReadAsStringAsync(ct);try{using var doc=JsonDocument.Parse(body);var root=doc.RootElement;throw new InvalidOperationException((root.TryGetProperty("detail",out var d)?d.GetString():null)??(root.TryGetProperty("title",out var t)?t.GetString():null)??$"HTTP {(int)r.StatusCode}");}catch(JsonException){throw new InvalidOperationException($"HTTP {(int)r.StatusCode}: {body}");}}
}
