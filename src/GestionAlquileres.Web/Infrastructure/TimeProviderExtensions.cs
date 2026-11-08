namespace GestionAlquileres.Web.Infrastructure;

/// <summary>
/// Acceso a "hoy" y "ahora" en hora argentina a partir de <see cref="TimeProvider"/>,
/// para que la lógica dependiente de la fecha sea testeable.
/// </summary>
public static class TimeProviderExtensions
{
    public static DateTime AhoraUtc(this TimeProvider reloj) => reloj.GetUtcNow().UtcDateTime;

    public static DateTime AhoraEnArgentina(this TimeProvider reloj) =>
        CulturaArgentina.AHoraArgentina(reloj.AhoraUtc());

    public static DateOnly HoyEnArgentina(this TimeProvider reloj) =>
        DateOnly.FromDateTime(reloj.AhoraEnArgentina());
}
