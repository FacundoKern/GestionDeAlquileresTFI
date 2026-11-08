using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using GestionAlquileres.Web.Models.Enums;

namespace GestionAlquileres.Web.Infrastructure;

/// <summary>Formatos argentinos para mostrar en vistas, PDFs y exportaciones.</summary>
public static class Formato
{
    public const string FormatoFecha = "dd/MM/yyyy";

    public static string Fecha(DateOnly fecha) => fecha.ToString(FormatoFecha, CultureInfo.InvariantCulture);

    public static string Fecha(DateOnly? fecha) => fecha is null ? "—" : Fecha(fecha.Value);

    /// <summary>Convierte un instante UTC a hora argentina: "dd/MM/yyyy HH:mm".</summary>
    public static string FechaHora(DateTime utc) =>
        CulturaArgentina.AHoraArgentina(utc).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

    public static string FechaHora(DateTime? utc) => utc is null ? "—" : FechaHora(utc.Value);

    /// <summary>"$ 1.234,56" o "US$ 1.234,56". Nunca se suman montos de distinta moneda.</summary>
    public static string Moneda(decimal monto, Moneda moneda) =>
        $"{SimboloMoneda(moneda)} {monto.ToString("N2", CulturaArgentina.Cultura)}";

    public static string SimboloMoneda(Moneda moneda) => moneda == Models.Enums.Moneda.Usd ? "US$" : "$";

    public static string Numero(decimal valor, int decimales = 2) =>
        valor.ToString("N" + decimales, CulturaArgentina.Cultura);

    public static string Porcentaje(decimal valor) =>
        valor.ToString("0.##", CulturaArgentina.Cultura) + " %";

    /// <summary>Nombre visible de un valor de enum (atributo [Display]).</summary>
    public static string Nombre<TEnum>(this TEnum valor) where TEnum : struct, Enum
    {
        var miembro = typeof(TEnum).GetMember(valor.ToString()).FirstOrDefault();
        return miembro?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? valor.ToString();
    }
}
