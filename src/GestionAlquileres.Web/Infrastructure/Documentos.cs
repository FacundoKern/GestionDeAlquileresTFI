using GestionAlquileres.Web.Models.Enums;

namespace GestionAlquileres.Web.Infrastructure;

/// <summary>Normalización, validación, formato y enmascarado de documentos argentinos.</summary>
public static class Documentos
{
    private static readonly int[] PesosCuit = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];

    /// <summary>Quita puntos, guiones y espacios; pasa a mayúsculas (pasaportes).</summary>
    public static string Normalizar(string? numero) =>
        new((numero ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    /// <summary>Devuelve null si es válido, o el mensaje de error.</summary>
    public static string? Validar(TipoDocumento tipo, string? numero)
    {
        var normalizado = Normalizar(numero);
        if (normalizado.Length == 0)
        {
            return "Ingresá el número de documento.";
        }

        return tipo switch
        {
            TipoDocumento.Dni when !(normalizado.All(char.IsDigit) && normalizado.Length is 7 or 8) =>
                "El DNI debe tener 7 u 8 dígitos.",
            TipoDocumento.Cuit or TipoDocumento.Cuil when !EsCuitValido(normalizado) =>
                $"El {(tipo == TipoDocumento.Cuit ? "CUIT" : "CUIL")} no es válido: deben ser 11 dígitos con dígito verificador correcto.",
            TipoDocumento.Pasaporte when normalizado.Length is < 6 or > 20 =>
                "El pasaporte debe tener entre 6 y 20 caracteres.",
            _ => null,
        };
    }

    /// <summary>Valida longitud y dígito verificador de un CUIT/CUIL (módulo 11).</summary>
    public static bool EsCuitValido(string? cuit)
    {
        var digitos = Normalizar(cuit);
        if (digitos.Length != 11 || !digitos.All(char.IsDigit))
        {
            return false;
        }

        return CalcularDigitoVerificadorCuit(digitos[..10]) == digitos[10] - '0';
    }

    /// <summary>Calcula el dígito verificador para un prefijo de 10 dígitos (útil para datos de prueba).</summary>
    public static int CalcularDigitoVerificadorCuit(string diezDigitos)
    {
        var suma = 0;
        for (var i = 0; i < 10; i++)
        {
            suma += (diezDigitos[i] - '0') * PesosCuit[i];
        }

        var verificador = 11 - (suma % 11);
        return verificador switch
        {
            11 => 0,
            10 => 9,
            _ => verificador,
        };
    }

    /// <summary>DNI "12.345.678", CUIT/CUIL "20-12345678-3", pasaporte sin cambios.</summary>
    public static string Formatear(TipoDocumento tipo, string numero)
    {
        var n = Normalizar(numero);
        return tipo switch
        {
            TipoDocumento.Dni when n.All(char.IsDigit) && n.Length > 0 =>
                long.Parse(n).ToString("#,0", CulturaArgentina.Cultura),
            TipoDocumento.Cuit or TipoDocumento.Cuil when n.Length == 11 =>
                $"{n[..2]}-{n[2..10]}-{n[10]}",
            _ => n,
        };
    }

    /// <summary>
    /// Versión enmascarada para usuarios sin permiso de ver datos personales completos:
    /// solo quedan visibles los últimos 3 caracteres (p. ej. "**.***.678").
    /// </summary>
    public static string Enmascarar(TipoDocumento tipo, string numero)
    {
        var formateado = Formatear(tipo, numero);
        var visibles = 3;
        var resultado = formateado.ToCharArray();
        var contados = 0;
        for (var i = resultado.Length - 1; i >= 0; i--)
        {
            if (!char.IsLetterOrDigit(resultado[i]))
            {
                continue;
            }

            if (contados >= visibles)
            {
                resultado[i] = '*';
            }

            contados++;
        }

        return new string(resultado);
    }
}
