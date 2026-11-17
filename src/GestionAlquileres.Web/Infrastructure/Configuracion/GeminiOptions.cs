namespace GestionAlquileres.Web.Infrastructure.Configuracion;

/// <summary>Asistente de ayuda con la API de Google Gemini (sección "Gemini" de appsettings).</summary>
public class GeminiOptions
{
    public const string Seccion = "Gemini";

    /// <summary>
    /// Clave de la API. NUNCA en el repositorio: User Secrets en desarrollo (Gemini:ClaveApi) o la variable de
    /// entorno Gemini__ClaveApi en producción. Sin clave, el asistente avisa que no está disponible.
    /// </summary>
    public string? ClaveApi { get; set; }

    /// <summary>Modelo de Gemini. Configurable porque Google retira modelos seguido (2.5 Flash se retiró en 2026).</summary>
    public string Modelo { get; set; } = "gemini-3.5-flash-lite";

    public int TimeoutSegundos { get; set; } = 20;

    /// <summary>Consultas por minuto que puede hacer cada usuario (el plan gratis de Gemini tiene un límite por minuto).</summary>
    public int ConsultasPorMinutoPorUsuario { get; set; } = 5;

    /// <summary>Largo máximo de la pregunta, en caracteres.</summary>
    public int LargoMaximoPregunta { get; set; } = 500;
}
