using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GestionAlquileres.Web.Infrastructure.Configuracion;
using Microsoft.Extensions.Options;

namespace GestionAlquileres.Web.Services.Asistente;

public record RespuestaAsistente(bool Exito, string Texto);

/// <summary>Asistente de ayuda sobre el uso del sistema, con la API de Google Gemini.</summary>
public interface IAsistenteService
{
    Task<RespuestaAsistente> ResponderAsync(string? pregunta, string? rol, CancellationToken ct = default);
}

/// <summary>
/// Llama a la API REST de Gemini (generateContent) con HttpClient, sin SDK. Cada pregunta va sola, con la guía del
/// sistema como instrucciones: el asistente no ve la base de datos ni guarda conversaciones. Ni la pregunta ni la
/// respuesta se escriben en los logs, porque el usuario podría haber escrito un dato personal igual.
/// </summary>
public sealed class AsistenteService : IAsistenteService
{
    public const string NoDisponible = "El asistente no está disponible en este momento. Probá de nuevo en unos minutos.";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly GeminiOptions _opciones;
    private readonly ILogger<AsistenteService> _logger;

    public AsistenteService(HttpClient http, IOptions<GeminiOptions> opciones, ILogger<AsistenteService> logger)
    {
        _http = http;
        _opciones = opciones.Value;
        _logger = logger;
    }

    public async Task<RespuestaAsistente> ResponderAsync(string? pregunta, string? rol, CancellationToken ct = default)
    {
        pregunta = pregunta?.Trim();
        if (string.IsNullOrEmpty(pregunta))
        {
            return new RespuestaAsistente(false, "Escribí tu pregunta.");
        }

        if (pregunta.Length > _opciones.LargoMaximoPregunta)
        {
            return new RespuestaAsistente(false, $"La pregunta es muy larga: escribila en {_opciones.LargoMaximoPregunta} caracteres o menos.");
        }

        if (FiltroDatosPersonales.Detectar(pregunta) is { } dato)
        {
            return new RespuestaAsistente(false,
                $"Por privacidad, la pregunta no se envió: parece tener {dato}. Preguntá sin datos de personas " +
                "(por ejemplo, \"¿cómo busco un inquilino por documento?\").");
        }

        if (string.IsNullOrWhiteSpace(_opciones.ClaveApi))
        {
            _logger.LogWarning("El asistente no tiene configurada la clave de Gemini (Gemini:ClaveApi)");
            return new RespuestaAsistente(false, "El asistente todavía no está configurado. Avisale al administrador del sistema.");
        }

        var pedido = new PedidoGemini(
            new Contenido(null, [new Parte(GuiaDelSistema.Instrucciones(rol))]),
            [new Contenido("user", [new Parte(pregunta)])],
            new ConfiguracionGeneracion(Temperature: 0.3, MaxOutputTokens: 1024));

        using var mensaje = new HttpRequestMessage(HttpMethod.Post, $"v1beta/models/{Uri.EscapeDataString(_opciones.Modelo)}:generateContent")
        {
            Content = JsonContent.Create(pedido, options: Json),
        };
        mensaje.Headers.Add("x-goog-api-key", _opciones.ClaveApi);

        try
        {
            using var respuesta = await _http.SendAsync(mensaje, ct);
            if (respuesta.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Gemini rechazó la consulta por límite de uso (429)");
                return new RespuestaAsistente(false, "El asistente recibió muchas consultas. Esperá un minuto y probá de nuevo.");
            }

            if (!respuesta.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini respondió {Estado} con el modelo {Modelo}", (int)respuesta.StatusCode, _opciones.Modelo);
                return new RespuestaAsistente(false, NoDisponible);
            }

            var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaGemini>(Json, ct);
            var texto = string.Concat(cuerpo?.Candidates?.FirstOrDefault()?.Content?.Parts?.Select(p => p.Text) ?? []).Trim();
            if (texto.Length == 0)
            {
                _logger.LogWarning("Gemini respondió sin texto (motivo: {Motivo})", cuerpo?.Candidates?.FirstOrDefault()?.FinishReason ?? "sin candidatos");
                return new RespuestaAsistente(false, "El asistente no pudo responder esa pregunta. Probá escribirla de otra forma.");
            }

            return new RespuestaAsistente(true, texto);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException && !ct.IsCancellationRequested)
        {
            _logger.LogError("No se pudo consultar a Gemini: {Tipo}", ex.GetType().Name);
            return new RespuestaAsistente(false, NoDisponible);
        }
    }

    private sealed record PedidoGemini(
        [property: JsonPropertyName("system_instruction")] Contenido SystemInstruction,
        IReadOnlyList<Contenido> Contents,
        ConfiguracionGeneracion GenerationConfig);

    private sealed record Contenido(string? Role, IReadOnlyList<Parte>? Parts);

    private sealed record Parte(string? Text);

    private sealed record ConfiguracionGeneracion(double Temperature, int MaxOutputTokens);

    private sealed record RespuestaGemini(IReadOnlyList<Candidato>? Candidates);

    private sealed record Candidato(Contenido? Content, string? FinishReason);
}
