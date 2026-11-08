namespace GestionAlquileres.Web.Security;

/// <summary>
/// Agrega los headers de seguridad a todas las respuestas.
/// La CSP no permite scripts ni estilos inline: todo JS/CSS vive en wwwroot.
/// </summary>
public sealed class EncabezadosSeguridadMiddleware
{
    private const string CspBase =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; " +
        "object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

    private readonly RequestDelegate _siguiente;
    private readonly string _csp;

    public EncabezadosSeguridadMiddleware(RequestDelegate siguiente, IWebHostEnvironment entorno)
    {
        _siguiente = siguiente;
        // En desarrollo, la recarga en caliente usa WebSockets en otro puerto.
        _csp = entorno.IsDevelopment()
            ? CspBase + "; connect-src 'self' ws://localhost:* wss://localhost:*"
            : CspBase + "; connect-src 'self'";
    }

    public Task InvokeAsync(HttpContext contexto)
    {
        contexto.Response.OnStarting(() =>
        {
            var headers = contexto.Response.Headers;
            headers.ContentSecurityPolicy = _csp;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            return Task.CompletedTask;
        });

        return _siguiente(contexto);
    }
}
