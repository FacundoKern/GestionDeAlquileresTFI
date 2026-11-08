namespace GestionAlquileres.Web.ViewModels.Comun;

/// <summary>Nivel del semáforo de estado de un contrato.</summary>
public enum NivelSemaforo
{
    /// <summary>Sin contrato vigente, finalizado o rescindido.</summary>
    Neutro = 0,

    /// <summary>Al día.</summary>
    AlDia = 1,

    /// <summary>Por vencer o con pago próximo.</summary>
    Atencion = 2,

    /// <summary>En mora o vencido.</summary>
    Critico = 3,
}

/// <summary>
/// Estado visual de un contrato. Siempre se muestra con texto e ícono además del color
/// (la información nunca se transmite solo por color).
/// </summary>
public record SemaforoViewModel(NivelSemaforo Nivel, string Texto, string? Detalle = null)
{
    public string Icono => Nivel switch
    {
        NivelSemaforo.AlDia => "✓",
        NivelSemaforo.Atencion => "!",
        NivelSemaforo.Critico => "✕",
        _ => "–",
    };

    public string ClaseCss => Nivel switch
    {
        NivelSemaforo.AlDia => "semaforo-al-dia",
        NivelSemaforo.Atencion => "semaforo-atencion",
        NivelSemaforo.Critico => "semaforo-critico",
        _ => "semaforo-neutro",
    };
}
