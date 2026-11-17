using System.Text.RegularExpressions;

namespace GestionAlquileres.Web.Services.Asistente;

/// <summary>
/// Ley 25.326: el asistente no manda datos personales a la API de Gemini. Antes de enviar una pregunta se busca
/// lo que parece un dato personal identificable (documento, CUIT, teléfono, email) y, si aparece, la pregunta
/// no se envía. Es una red de seguridad, no un detector perfecto: por eso la pantalla también lo pide.
/// Función pura: se prueba sin red ni base.
/// </summary>
public static partial class FiltroDatosPersonales
{
    /// <summary>Devuelve qué tipo de dato personal parece tener el texto, o null si no se encontró ninguno.</summary>
    public static string? Detectar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        if (Email().IsMatch(texto))
        {
            return "un email";
        }

        // Secuencias de 7 o más dígitos, aunque estén separadas por puntos, guiones o espacios:
        // DNI (20.345.678), CUIT (20-34567890-1) o teléfono (11 5555-0101).
        // Los montos con signo de moneda ($ 1.850.000) no son datos personales.
        foreach (Match grupo in GrupoNumerico().Matches(texto))
        {
            var digitos = grupo.Value.Count(char.IsDigit);
            if (digitos >= 7 && !EsMonto(texto, grupo.Index))
            {
                return "un número de documento, CUIT o teléfono";
            }
        }

        return null;
    }

    private static bool EsMonto(string texto, int inicio)
    {
        var i = inicio - 1;
        while (i >= 0 && char.IsWhiteSpace(texto[i]))
        {
            i--;
        }

        return i >= 0 && texto[i] == '$';
    }

    [GeneratedRegex(@"[\w.+-]+@[\w-]+\.[\w.-]+", RegexOptions.CultureInvariant)]
    private static partial Regex Email();

    [GeneratedRegex(@"\d[\d.\-\s]{5,}\d", RegexOptions.CultureInvariant)]
    private static partial Regex GrupoNumerico();
}
