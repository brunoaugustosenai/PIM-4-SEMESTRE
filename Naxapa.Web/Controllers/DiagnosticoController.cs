using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Naxapa.Web.Services;

namespace Naxapa.Web.Controllers;

[AllowAnonymous]
public sealed class DiagnosticoController(DatabaseRuntimeState databaseState, IConfiguration configuration) : Controller
{
    public IActionResult Index()
    {
        ViewBag.Ready = databaseState.IsReady;
        ViewBag.Error = databaseState.ErrorMessage;
        ViewBag.Connection = configuration.GetConnectionString("NaxapaDb")?.Replace("TrustServerCertificate=True", "TrustServerCertificate=***", StringComparison.OrdinalIgnoreCase);
        return View();
    }

    [HttpGet("/health")]
    public IActionResult Health()
        => databaseState.IsReady
            ? Ok(new { status = "ok", database = "NaxapaDB" })
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "erro",
                database = "NaxapaDB",
                error = databaseState.ErrorMessage
            });
}
