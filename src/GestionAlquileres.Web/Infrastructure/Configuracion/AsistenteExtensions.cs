using System.Threading.RateLimiting;
using GestionAlquileres.Web.Services.Asistente;
using Microsoft.AspNetCore.RateLimiting;

namespace GestionAlquileres.Web.Infrastructure.Configuracion;

public static class AsistenteExtensions
{
    public const string PoliticaRateLimitAsistente = "asistente";

    /// <summary>Asistente con Gemini: cliente HTTP con timeout y límite de consultas por usuario.</summary>
    public static IServiceCollection AgregarAsistente(this IServiceCollection services, IConfiguration configuracion)
    {
        var seccion = configuracion.GetSection(GeminiOptions.Seccion);
        services.Configure<GeminiOptions>(seccion);
        var opciones = seccion.Get<GeminiOptions>() ?? new GeminiOptions();

        services.AddHttpClient<IAsistenteService, AsistenteService>(cliente =>
        {
            cliente.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
            cliente.Timeout = TimeSpan.FromSeconds(opciones.TimeoutSegundos);
        });

        // Por usuario (no por IP): protege la cuota gratuita de Gemini y evita que un usuario la agote solo.
        services.Configure<RateLimiterOptions>(limitador => limitador.AddPolicy(PoliticaRateLimitAsistente, contexto =>
            RateLimitPartition.GetFixedWindowLimiter(
                contexto.User.Identity?.Name ?? "anonimo",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = opciones.ConsultasPorMinutoPorUsuario,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                })));

        return services;
    }
}
