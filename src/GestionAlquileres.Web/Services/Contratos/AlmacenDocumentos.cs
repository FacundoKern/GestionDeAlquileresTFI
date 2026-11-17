using GestionAlquileres.Web.Infrastructure.Configuracion;
using Microsoft.Extensions.Options;

namespace GestionAlquileres.Web.Services.Contratos;

/// <summary>Dónde se guardan los PDF de los contratos. Una interfaz para poder cambiarlo (p. ej. a un blob) sin tocar el servicio.</summary>
public interface IAlmacenDocumentos
{
    Task GuardarAsync(string archivo, byte[] contenido, CancellationToken ct = default);

    /// <summary>Ruta física del archivo, o null si no existe.</summary>
    string? Ruta(string archivo);
}

public sealed class AlmacenDocumentosEnDisco : IAlmacenDocumentos
{
    private readonly string _carpeta;

    public AlmacenDocumentosEnDisco(IOptions<DocumentosOptions> opciones, IWebHostEnvironment entorno)
    {
        var carpeta = opciones.Value.Carpeta;
        _carpeta = Path.GetFullPath(Path.IsPathRooted(carpeta) ? carpeta : Path.Combine(entorno.ContentRootPath, carpeta));
    }

    public async Task GuardarAsync(string archivo, byte[] contenido, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_carpeta);
        await File.WriteAllBytesAsync(RutaArchivo(archivo), contenido, ct);
    }

    public string? Ruta(string archivo)
    {
        var ruta = RutaArchivo(archivo);
        return File.Exists(ruta) ? ruta : null;
    }

    // El nombre lo genera el sistema (un GUID): nunca viene del usuario, así que no hay riesgo de "../".
    private string RutaArchivo(string archivo) => Path.Combine(_carpeta, archivo + ".pdf");
}

/// <summary>Validación del archivo subido. Función pura: se prueba sin disco ni base.</summary>
public static class ValidadorPdf
{
    private static readonly byte[] Firma = "%PDF-"u8.ToArray();

    /// <summary>
    /// Devuelve el motivo por el que el archivo no se acepta, o null si es un PDF válido. El tipo se decide por el
    /// contenido (la firma "%PDF-" del principio), no por la extensión ni por el Content-Type que manda el navegador.
    /// </summary>
    public static string? Validar(ReadOnlySpan<byte> contenido, long tamanioMaximoBytes)
    {
        if (contenido.Length == 0)
        {
            return "El archivo está vacío.";
        }

        if (contenido.Length > tamanioMaximoBytes)
        {
            return $"El archivo pesa más de {tamanioMaximoBytes / (1024 * 1024)} MB.";
        }

        return contenido.StartsWith(Firma) ? null : "El archivo no es un PDF. Solo se aceptan documentos PDF.";
    }
}
