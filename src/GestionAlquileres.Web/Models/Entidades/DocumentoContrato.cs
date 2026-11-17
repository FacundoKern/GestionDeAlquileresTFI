using GestionAlquileres.Web.Models.Base;

namespace GestionAlquileres.Web.Models.Entidades;

/// <summary>
/// PDF adjunto a un contrato (el contrato firmado escaneado, un anexo, un inventario). Como las fotos, el archivo
/// no está en la base sino en el almacenamiento de documentos, fuera de wwwroot: <see cref="Archivo"/> es el
/// nombre generado por el sistema, nunca el nombre que trajo el archivo subido.
/// </summary>
public class DocumentoContrato : EntidadNegocio
{
    public int ContratoId { get; set; }

    public Contrato Contrato { get; set; } = null!;

    /// <summary>Nombre del archivo en disco (un GUID, sin extensión: siempre es un PDF).</summary>
    public string Archivo { get; set; } = string.Empty;

    /// <summary>Qué es el documento, p. ej. "Contrato firmado".</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Nombre del archivo que se subió (solo informativo).</summary>
    public string? NombreOriginal { get; set; }

    public long TamanioBytes { get; set; }
}
