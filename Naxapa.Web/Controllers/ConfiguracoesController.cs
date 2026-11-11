using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Naxapa.Web.Data;
using Naxapa.Web.Models;
using Naxapa.Web.Security;
using Naxapa.Web.Services;

namespace Naxapa.Web.Controllers;

public sealed class ConfiguracoesController(
    NaxapaDbContext db,
    CurrentUserService currentUser,
    DatabaseRuntimeState databaseState,
    IConfiguration configuration) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var connection = new SqlConnectionStringBuilder(configuration.GetConnectionString("NaxapaDb"));
        var establishmentName = await db.Estabelecimentos.AsNoTracking()
            .Where(x => x.EstabelecimentoId == currentUser.EstabelecimentoId)
            .Select(x => x.NomeFantasia)
            .SingleAsync(cancellationToken);

        return View(new SettingsViewModel
        {
            DatabaseReady = databaseState.IsReady,
            DatabaseStatus = databaseState.IsReady ? "Conectado" : databaseState.ErrorMessage ?? "Falha na conexão",
            Server = connection.DataSource,
            Database = connection.InitialCatalog,
            Estabelecimento = establishmentName
        });
    }
}
