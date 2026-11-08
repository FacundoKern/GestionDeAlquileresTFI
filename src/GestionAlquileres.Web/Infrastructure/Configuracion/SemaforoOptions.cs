namespace GestionAlquileres.Web.Infrastructure.Configuracion;

/// <summary>
/// Umbrales del semáforo y del tablero (sección "Semaforo" de appsettings).
/// TODO (fase 4): hacerlos editables desde Configuración.
/// </summary>
public class SemaforoOptions
{
    public const string Seccion = "Semaforo";

    /// <summary>Un contrato vigente que termina dentro de estos días está "por vencer".</summary>
    public int DiasContratoPorVencer { get; set; } = 60;

    /// <summary>Una cuota pendiente que vence dentro de estos días es un "pago próximo".</summary>
    public int DiasPagoProximo { get; set; } = 5;

    /// <summary>Un ajuste de precio que corresponde dentro de estos días es un "ajuste próximo".</summary>
    public int DiasProximoAjuste { get; set; } = 30;
}
