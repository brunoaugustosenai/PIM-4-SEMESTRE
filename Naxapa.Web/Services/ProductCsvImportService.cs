using System.Globalization;
using System.Text;

namespace Naxapa.Web.Services;

public sealed record ParsedProductCsvRow(
    int Line,
    string? CodigoInterno,
    string? CodigoBarras,
    string Nome,
    string? Descricao,
    string Categoria,
    decimal? PrecoVenda,
    string? Setor,
    string UnidadeMedida);

public sealed class ProductCsvImportService(IWebHostEnvironment environment)
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0 || file.Length > MaxFileSize)
            throw new InvalidOperationException("O CSV deve possuir entre 1 byte e 5 MB.");
        if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Envie um arquivo com extensão .csv.");

        var directory = Path.Combine(environment.ContentRootPath, "App_Data", "imports");
        Directory.CreateDirectory(directory);
        var token = Guid.NewGuid().ToString("N");
        var path = Path.Combine(directory, token + ".csv");
        await using var output = File.Create(path);
        await file.CopyToAsync(output, cancellationToken);
        return token;
    }

    public async Task<IReadOnlyList<ParsedProductCsvRow>> ParseAsync(string token, CancellationToken cancellationToken)
    {
        var path = ResolveToken(token);
        if (!File.Exists(path))
            throw new InvalidOperationException("Arquivo de importação expirado ou inexistente.");
        var lines = await File.ReadAllLinesAsync(path, Encoding.UTF8, cancellationToken);
        if (lines.Length < 2)
            throw new InvalidOperationException("O CSV precisa possuir cabeçalho e ao menos uma linha de produto.");

        var delimiter = DetectDelimiter(lines[0]);
        var headers = ParseCsvLine(lines[0], delimiter).Select(NormalizeHeader).ToArray();
        var requiredName = Array.FindIndex(headers, x => x == "nome");
        var requiredCategory = Array.FindIndex(headers, x => x == "categoria");
        var requiredPrice = Array.FindIndex(headers, x => x is "preco" or "precovenda");
        if (requiredName < 0 || requiredCategory < 0 || requiredPrice < 0)
            throw new InvalidOperationException("Cabeçalho obrigatório: Nome, Categoria e Preco/PrecoVenda.");

        var rows = new List<ParsedProductCsvRow>();
        for (var i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var values = ParseCsvLine(lines[i], delimiter);
            string? Get(params string[] names)
            {
                foreach (var name in names)
                {
                    var idx = Array.FindIndex(headers, x => x == name);
                    if (idx >= 0 && idx < values.Count)
                    {
                        var value = values[idx].Trim();
                        if (!string.IsNullOrEmpty(value)) return value;
                    }
                }
                return null;
            }

            decimal? price = null;
            var priceText = Get("preco", "precovenda");
            if (priceText is not null)
            {
                priceText = priceText.Replace("R$", "", StringComparison.OrdinalIgnoreCase).Trim();
                if (decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.GetCultureInfo("pt-BR"), out var br)) price = br;
                else if (decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.InvariantCulture, out var inv)) price = inv;
            }

            rows.Add(new ParsedProductCsvRow(
                i + 1,
                Get("codigo", "codigointerno"),
                Get("codigobarras", "ean", "gtin"),
                Get("nome") ?? string.Empty,
                Get("descricao"),
                Get("categoria") ?? string.Empty,
                price,
                Get("setor", "setorproducao"),
                Get("unidade", "unidademedida") ?? "UN"));
        }
        return rows;
    }

    public void Delete(string token)
    {
        var path = ResolveToken(token);
        if (File.Exists(path)) File.Delete(path);
    }

    private string ResolveToken(string token)
    {
        if (!Guid.TryParseExact(token, "N", out _))
            throw new InvalidOperationException("Token de importação inválido.");
        return Path.Combine(environment.ContentRootPath, "App_Data", "imports", token + ".csv");
    }

    private static char DetectDelimiter(string header)
    {
        var semicolon = header.Count(c => c == ';');
        var comma = header.Count(c => c == ',');
        return semicolon >= comma ? ';' : ',';
    }

    private static string NormalizeHeader(string text)
    {
        var normalized = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
            if (char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c)) sb.Append(c);
        return sb.ToString();
    }

    private static List<string> ParseCsvLine(string line, char delimiter)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (c == delimiter && !quoted)
            {
                result.Add(sb.ToString()); sb.Clear();
            }
            else sb.Append(c);
        }
        result.Add(sb.ToString());
        return result;
    }
}
