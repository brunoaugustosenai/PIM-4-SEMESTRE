namespace Naxapa.Web.Services;

public sealed class NaxapaClock
{
    private static readonly TimeZoneInfo BusinessTimeZone = ResolveTimeZone();

    public DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BusinessTimeZone);

    public (DateTime StartUtc, DateTime EndUtc, string Label) GetUtcRange(
        string? periodo,
        DateTime? dataInicial = null,
        DateTime? dataFinal = null)
    {
        var today = LocalNow.Date;
        DateTime startLocal;
        DateTime endLocal;
        string label;

        switch ((periodo ?? "hoje").Trim().ToLowerInvariant())
        {
            case "ontem":
                startLocal = today.AddDays(-1);
                endLocal = today;
                label = "Ontem";
                break;
            case "7dias":
            case "ultimos7dias":
                startLocal = today.AddDays(-6);
                endLocal = today.AddDays(1);
                label = "Últimos 7 dias";
                break;
            case "mes":
            case "mesatual":
                startLocal = new DateTime(today.Year, today.Month, 1);
                endLocal = startLocal.AddMonths(1);
                label = "Mês atual";
                break;
            case "personalizado":
                startLocal = (dataInicial ?? today).Date;
                var inclusiveEnd = (dataFinal ?? dataInicial ?? today).Date;
                if (inclusiveEnd < startLocal)
                    inclusiveEnd = startLocal;
                endLocal = inclusiveEnd.AddDays(1);
                label = $"{startLocal:dd/MM/yyyy} a {inclusiveEnd:dd/MM/yyyy}";
                break;
            default:
                startLocal = today;
                endLocal = today.AddDays(1);
                label = "Hoje";
                break;
        }

        return (
            TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(startLocal, DateTimeKind.Unspecified), BusinessTimeZone),
            TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(endLocal, DateTimeKind.Unspecified), BusinessTimeZone),
            label);
    }

    public DateTime ToLocal(DateTime utc)
    {
        var normalized = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(normalized, BusinessTimeZone);
    }

    private static TimeZoneInfo ResolveTimeZone()
    {
        foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.Utc;
    }
}
