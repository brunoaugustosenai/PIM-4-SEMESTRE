using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Naxapa.Web.Data;

public sealed class DatabaseInitializer(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("NaxapaDb")
            ?? throw new InvalidOperationException("ConnectionStrings:NaxapaDb não foi configurada.");
        var autoCreate = configuration.GetValue("Naxapa:AutoCreateDatabase", true);
        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new InvalidOperationException("A string de conexão deve informar o banco NaxapaDB.");

        builder.InitialCatalog = "master";
        await using (var master = new SqlConnection(builder.ConnectionString))
        {
            await master.OpenAsync(cancellationToken);
            await using var existsCommand = new SqlCommand("SELECT CASE WHEN DB_ID(@databaseName) IS NULL THEN 0 ELSE 1 END;", master);
            existsCommand.Parameters.AddWithValue("@databaseName", databaseName);
            var exists = Convert.ToInt32(await existsCommand.ExecuteScalarAsync(cancellationToken)) == 1;
            if (!exists)
            {
                if (!autoCreate)
                    throw new InvalidOperationException($"O banco {databaseName} não existe e a criação automática está desativada.");
                logger.LogInformation("NaxapaDB não encontrado. Criando a estrutura v2...");
                await ExecuteScriptFileAsync(master, "NaxapaDB_Completo_v2_COM_CAIXA.sql", cancellationToken);
            }
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var requiredBaseObjects = new[] { "cad.Produto", "cad.Cliente", "seg.Usuario", "op.Pedido", "fin.Pagamento" };
        foreach (var objectName in requiredBaseObjects)
        {
            if (!await ObjectExistsAsync(connection, objectName, "U", cancellationToken))
                throw new InvalidOperationException($"O NaxapaDB está incompleto: objeto obrigatório {objectName} não foi encontrado. Faça backup e execute o script de implantação correspondente.");
        }

        var cashReady = await ObjectExistsAsync(connection, "fin.Caixa", "U", cancellationToken)
                        && await ObjectExistsAsync(connection, "rel.vw_CaixaResumo", "V", cancellationToken);
        if (!cashReady)
        {
            logger.LogInformation("Estrutura de Caixa não encontrada. Aplicando patch v2...");
            await ExecuteScriptFileAsync(connection, "NaxapaDB_Modulo_Caixa_v2.sql", cancellationToken);
        }

        // O patch v3 é idempotente. Executá-lo em toda inicialização garante que
        // permissões/seeds adicionados posteriormente sejam reconciliados sem apagar dados.
        logger.LogInformation("Validando/aplicando patch v3 idempotente...");
        await ExecuteScriptFileAsync(connection, "NaxapaDB_Patch_v3.sql", cancellationToken);
    }

    private async Task ExecuteScriptFileAsync(SqlConnection connection, string fileName, CancellationToken cancellationToken)
    {
        var scriptPath = Path.Combine(environment.ContentRootPath, "Database", fileName);
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException($"Script de banco não encontrado: {fileName}", scriptPath);

        var script = await File.ReadAllTextAsync(scriptPath, cancellationToken);
        var batches = Regex.Split(script, @"^\s*GO\s*;?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch)) continue;
            await using var command = connection.CreateCommand();
            command.CommandText = batch;
            command.CommandTimeout = 120;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task<bool> ObjectExistsAsync(SqlConnection connection, string objectName, string objectType, CancellationToken ct)
    {
        await using var command = new SqlCommand("SELECT CASE WHEN OBJECT_ID(@objectName, @objectType) IS NULL THEN 0 ELSE 1 END;", connection);
        command.Parameters.AddWithValue("@objectName", objectName);
        command.Parameters.AddWithValue("@objectType", objectType);
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct)) == 1;
    }

    private static async Task<bool> ColumnExistsAsync(SqlConnection connection, string tableName, string columnName, CancellationToken ct)
    {
        await using var command = new SqlCommand("SELECT CASE WHEN COL_LENGTH(@tableName, @columnName) IS NULL THEN 0 ELSE 1 END;", connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        command.Parameters.AddWithValue("@columnName", columnName);
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct)) == 1;
    }
}
