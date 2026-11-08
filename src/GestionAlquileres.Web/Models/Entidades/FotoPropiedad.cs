using GestionAlquileres.Web.Models.Base;

namespace GestionAlquileres.Web.Models.Entidades;

/// <summary>
/// Foto de una propiedad. El archivo no está en la base sino en el almacenamiento de fotos (fuera de wwwroot):
/// <see cref="Archivo"/> es el nombre generado por el sistema, nunca el nombre que trajo el archivo subido.
/// Se guarda ya procesada: sin metadatos (EXIF/GPS), orientada y reducida, más una miniatura.
/// </summary>
public class FotoPropiedad : EntidadNegocio
{
    public int PropiedadId { get; set; }

    public Propiedad Propiedad { get; set; } = null!;

    /// <summary>Nombre base del archivo (un GUID). La foto es "{Archivo}.jpg" y la miniatura "{Archivo}_m.jpg".</summary>
    public string Archivo { get; set; } = string.Empty;

    /// <summary>Texto alternativo para lectores de pantalla (p. ej. "Living con balcón").</summary>
    public string? Descripcion { get; set; }

    /// <summary>La foto que representa a la propiedad en los listados. Una por propiedad.</summary>
    public bool EsPrincipal { get; set; }

    public int Orden { get; set; }

    public int Ancho { get; set; }

    public int Alto { get; set; }

    public long TamanioBytes { get; set; }

    /// <summary>Nombre del archivo que se subió (solo informativo, para la auditoría).</summary>
    public string? NombreOriginal { get; set; }
}
