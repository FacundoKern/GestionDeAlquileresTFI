using System.Text.RegularExpressions;
using GestionAlquileres.Web.Infrastructure.Configuracion;
using Microsoft.Extensions.Options;

namespace GestionAlquileres.Web.Services.Fotos;

/// <summary>
/// Dónde se guardan los archivos de las fotos. Hoy es una carpeta del servidor; si se despliega en la nube, se
/// puede reemplazar por un almacenamiento de blobs sin tocar el resto del código.
/// </summary>
public interface IAlmacenFotos
{
    Task GuardarAsync(string archivo, ImagenProcesada imagen, CancellationToken ct = default);

    /// <summary>Ruta física de la foto o de su miniatura; null si no existe.</summary>
    string? RutaDe(string archivo, bool miniatura);
}

public sealed partial class AlmacenFotosEnDisco : IAlmacenFotos
{
    private readonly string _carpeta;

    public AlmacenFotosEnDisco(IOptions<FotosOptions> opciones, IWebHostEnvironment entorno)
    {
        var carpeta = opciones.Value.Carpeta;
        _carpeta = Path.GetFullPath(Path.IsPathRooted(carpeta) ? carpeta : Path.Combine(entorno.ContentRootPath, carpeta));
    }

    public async Task GuardarAsync(string archivo, ImagenProcesada imagen, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_carpeta);
        await File.WriteAllBytesAsync(Ruta(archivo, miniatura: false), imagen.Foto, ct);
        await File.WriteAllBytesAsync(Ruta(archivo, miniatura: true), imagen.Miniatura, ct);
    }

    public string? RutaDe(string archivo, bool miniatura)
    {
        var ruta = Ruta(archivo, miniatura);
        return File.Exists(ruta) ? ruta : null;
    }

    /// <summary>El nombre lo genera el sistema (un GUID), pero igual se valida: nunca se arma una ruta con texto arbitrario.</summary>
    private string Ruta(string archivo, bool miniatura)
    {
        if (!NombreValido().IsMatch(archivo))
        {
            throw new ArgumentException("Nombre de archivo de foto inválido.", nameof(archivo));
        }

        return Path.Combine(_carpeta, miniatura ? $"{archivo}_m.jpg" : $"{archivo}.jpg");
    }

    [GeneratedRegex("^[0-9a-f]{32}$")]
    private static partial Regex NombreValido();
}
