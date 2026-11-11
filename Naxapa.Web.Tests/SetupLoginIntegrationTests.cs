using System.Net;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Naxapa.Web.Tests;

public sealed class SetupLoginIntegrationTests : IAsyncLifetime
{
    private readonly string _databaseName = $"NaxapaDB_Test_{Guid.NewGuid():N}";
    private NaxapaTestFactory? _factory;

    public Task InitializeAsync()
    {
        if (!OperatingSystem.IsWindows())
            return Task.CompletedTask;

        _factory = new NaxapaTestFactory(_databaseName);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task FreshDatabase_Setup_Login_And_MainPages_Work()
    {
        // LocalDB é um componente Windows. Este teste é executado de verdade
        // no ambiente-alvo do NAXAPA (Windows + SQL Server LocalDB).
        if (!OperatingSystem.IsWindows())
            return;

        Assert.NotNull(_factory);
        using var setupClient = _factory!.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("http://localhost")
        });

        // Banco criado/inicializado pelo DatabaseInitializer.
        using (var health = await setupClient.GetAsync("/health"))
        {
            Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        }

        // Sem usuários: Login deve redirecionar para Setup.
        using (var loginBeforeSetup = await setupClient.GetAsync("/Conta/Login"))
        {
            Assert.True(IsRedirect(loginBeforeSetup.StatusCode));
            Assert.Contains("/Setup", loginBeforeSetup.Headers.Location?.OriginalString ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        // Abre Setup e coleta antiforgery token/cookie.
        using var setupPage = await setupClient.GetAsync("/Setup");
        Assert.Equal(HttpStatusCode.OK, setupPage.StatusCode);
        var setupHtml = await setupPage.Content.ReadAsStringAsync();
        var setupToken = ExtractAntiForgeryToken(setupHtml);

        const string userName = "admin.test";
        const string email = "admin.test@naxapa.local";
        const string password = "Naxapa@Teste#2026";

        using var setupPost = await setupClient.PostAsync("/Setup", Form(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = setupToken,
            ["Estabelecimento"] = "NAXAPA Teste Automatizado",
            ["CodigoEstabelecimento"] = "TESTE",
            ["Documento"] = "11222333000181",
            ["Nome"] = "Administrador Teste",
            ["UserName"] = userName,
            ["Email"] = email,
            ["Password"] = password,
            ["ConfirmPassword"] = password
        }));
        Assert.True(IsRedirect(setupPost.StatusCode), $"Setup retornou {(int)setupPost.StatusCode}.");
        await AssertCompanyDocumentAsync(_databaseName, "11222333000181");

        // Setup já autentica o novo administrador.
        using (var homeAfterSetup = await setupClient.GetAsync("/"))
        {
            Assert.Equal(HttpStatusCode.OK, homeAfterSetup.StatusCode);
        }

        using (var gtinResponse = await setupClient.GetAsync("/Produtos/GerarGtin13"))
        {
            Assert.Equal(HttpStatusCode.OK, gtinResponse.StatusCode);
            using var json = JsonDocument.Parse(await gtinResponse.Content.ReadAsStringAsync());
            var gtin = json.RootElement.GetProperty("gtin").GetString();
            Assert.NotNull(gtin);
            Assert.True(new Naxapa.Web.Services.Gtin13Service().IsValid(gtin));
        }

        // Novo cliente sem cookies: valida login de verdade.
        using var loginClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("http://localhost")
        });

        using var loginPage = await loginClient.GetAsync("/Conta/Login");
        Assert.Equal(HttpStatusCode.OK, loginPage.StatusCode);
        var loginHtml = await loginPage.Content.ReadAsStringAsync();
        var loginToken = ExtractAntiForgeryToken(loginHtml);

        using var loginPost = await loginClient.PostAsync("/Conta/Login", Form(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = loginToken,
            ["Login"] = userName,
            ["Password"] = password,
            ["RememberMe"] = "false",
            ["ReturnUrl"] = string.Empty
        }));
        Assert.True(IsRedirect(loginPost.StatusCode), $"Login retornou {(int)loginPost.StatusCode}.");

        var pages = new[]
        {
            "/",
            "/Produtos",
            "/Clientes",
            "/Cardapio",
            "/cardapio-digital?estabelecimentoId=1",
            "/Pedidos",
            "/Caixa",
            "/Configuracoes"
        };

        foreach (var page in pages)
        {
            using var response = await loginClient.GetAsync(page);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using var publicMenuClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
            BaseAddress = new Uri("http://localhost")
        });
        using (var publicMenu = await publicMenuClient.GetAsync("/cardapio-digital?estabelecimentoId=1"))
        {
            Assert.Equal(HttpStatusCode.OK, publicMenu.StatusCode);
        }

        // Setup deve ficar indisponível após a criação do primeiro usuário.
        using var setupAfterUser = await loginClient.GetAsync("/Setup");
        Assert.True(IsRedirect(setupAfterUser.StatusCode));
        Assert.Contains("/Conta/Login", setupAfterUser.Headers.Location?.OriginalString ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();

        if (!OperatingSystem.IsWindows())
            return;

        await DropDatabaseAsync(_databaseName);
    }

    private static FormUrlEncodedContent Form(Dictionary<string, string> values) => new(values);

    private static bool IsRedirect(HttpStatusCode statusCode)
        => statusCode is HttpStatusCode.MovedPermanently
            or HttpStatusCode.Redirect
            or HttpStatusCode.RedirectMethod
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;

    private static string ExtractAntiForgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            "name=\\\"__RequestVerificationToken\\\"[^>]*value=\\\"([^\\\"]+)\\\"",
            RegexOptions.IgnoreCase);
        Assert.True(match.Success, "Token antiforgery não encontrado no HTML.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static async Task AssertCompanyDocumentAsync(string databaseName, string expectedDocument)
    {
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;Connect Timeout=10";
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT TOP (1) Documento FROM cad.Estabelecimento ORDER BY EstabelecimentoId";
        var value = (string?)await command.ExecuteScalarAsync();
        Assert.Equal(expectedDocument, value);
    }

    private static async Task DropDatabaseAsync(string databaseName)
    {
        const string masterConnection = "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;Connect Timeout=10";
        await using var connection = new SqlConnection(masterConnection);
        await connection.OpenAsync();

        var safeName = databaseName.Replace("]", "]]", StringComparison.Ordinal);
        await using var command = connection.CreateCommand();
        command.CommandText = $"IF DB_ID(@db) IS NOT NULL BEGIN ALTER DATABASE [{safeName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{safeName}]; END";
        command.Parameters.AddWithValue("@db", databaseName);
        await command.ExecuteNonQueryAsync();
    }

    private sealed class NaxapaTestFactory(string databaseName) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                var connection = $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=10";
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:NaxapaDb"] = connection,
                    ["Naxapa:AutoCreateDatabase"] = "true"
                });
            });
        }
    }
}
