using System.Globalization;

namespace GestionAlquileres.Web.Infrastructure;

/// <summary>
/// Cultura y zona horaria fijas de la aplicación (Argentina).
/// Todas las fechas de auditoría se guardan en UTC y se convierten con <see cref="ZonaHoraria"/> solo al mostrarlas.
/// </summary>
public static class CulturaArgentina
{
    public const string NombreCultura = "es-AR";

    public static readonly CultureInfo Cultura = CrearCultura();

    public static readonly TimeZoneInfo ZonaHoraria = ObtenerZonaHoraria();

    private static CultureInfo CrearCultura()
    {
        var cultura = (CultureInfo)CultureInfo.GetCultureInfo(NombreCultura).Clone();
        cultura.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
        cultura.DateTimeFormat.DateSeparator = "/";
        cultura.NumberFormat.NumberDecimalSeparator = ",";
        cultura.NumberFormat.NumberGroupSeparator = ".";
        cultura.NumberFormat.CurrencyDecimalSeparator = ",";
        cultura.NumberFormat.CurrencyGroupSeparator = ".";
        cultura.NumberFormat.CurrencySymbol = "$";
        return CultureInfo.ReadOnly(cultura);
    }

    private static TimeZoneInfo ObtenerZonaHoraria()
    {
        // .NET 8 resuelve IDs IANA también en Windows (vía ICU); el ID de Windows queda como respaldo.
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");
        }
    }

    public static DateTime AHoraArgentina(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), ZonaHoraria);

    /// <summary>Instante UTC en que empieza (00:00, hora argentina) el día indicado. Para filtrar por fecha.</summary>
    public static DateTime InicioDelDiaEnUtc(DateOnly dia) =>
        TimeZoneInfo.ConvertTimeToUtc(dia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), ZonaHoraria);
}
