using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Entities;
using Naxapa.Web.Security;
using Naxapa.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
});
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();

var naxapaConnectionString = builder.Configuration.GetConnectionString("NaxapaDb")
    ?? throw new InvalidOperationException("ConnectionStrings:NaxapaDb não foi configurada.");

builder.Services.AddDbContext<NaxapaDbContext>(options =>
{
    // Mesmo padrão estável validado no projeto de referência: conexão SQL Server
    // direta pelo DbContext, sem retry automático em volta de transações manuais.
    options.UseSqlServer(naxapaConnectionString, sql => sql.CommandTimeout(30));

    if (builder.Environment.IsDevelopment())
        options.EnableDetailedErrors();
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = NaxapaAuthenticationSchemes.Cookie;
    options.DefaultChallengeScheme = NaxapaAuthenticationSchemes.Cookie;
})
.AddCookie(NaxapaAuthenticationSchemes.Cookie, options =>
{
    options.LoginPath = "/Conta/Login";
    options.AccessDeniedPath = "/Conta/AcessoNegado";
    options.Cookie.Name = "NAXAPA.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
})
.AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, ApiTokenAuthenticationHandler>(
    NaxapaAuthenticationSchemes.ApiBearer, _ => { });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddScoped<IPasswordHasher<UsuarioEntity>, PasswordHasher<UsuarioEntity>>();

builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<UserAuthenticationService>();
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<ProductCsvImportService>();
builder.Services.AddSingleton<Gtin13Service>();
builder.Services.AddScoped<OrderWorkflowService>();
builder.Services.AddSingleton<NaxapaClock>();
builder.Services.AddSingleton<DatabaseRuntimeState>();
builder.Services.AddSingleton<DatabaseInitializer>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("api", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var app = builder.Build();
var databaseState = app.Services.GetRequiredService<DatabaseRuntimeState>();
var initializer = app.Services.GetRequiredService<DatabaseInitializer>();
try
{
    await initializer.InitializeAsync();
    databaseState.MarkReady();
}
catch (Exception ex)
{
    databaseState.MarkFailed(ex.Message);
    app.Logger.LogError(ex, "Falha ao inicializar o NaxapaDB.");
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.Use(async (context, next) =>
{
    if (databaseState.IsReady)
    {
        await next();
        return;
    }

    var path = context.Request.Path;
    var allowed = path.StartsWithSegments("/Diagnostico", StringComparison.OrdinalIgnoreCase)
                  || path.StartsWithSegments("/css", StringComparison.OrdinalIgnoreCase)
                  || path.StartsWithSegments("/js", StringComparison.OrdinalIgnoreCase)
                  || path.StartsWithSegments("/uploads", StringComparison.OrdinalIgnoreCase)
                  || path.StartsWithSegments("/favicon", StringComparison.OrdinalIgnoreCase);
    if (!allowed)
    {
        context.Response.Redirect("/Diagnostico");
        return;
    }
    await next();
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi("/openapi/{documentName}.json");
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program;
