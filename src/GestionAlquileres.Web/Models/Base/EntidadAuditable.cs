namespace GestionAlquileres.Web.Models.Base;

/// <summary>
/// Entidad con campos de auditoría (quién y cuándo la creó/modificó) y control de concurrencia optimista.
/// Los campos de auditoría los completa automáticamente el DbContext; no se asignan a mano.
/// </summary>
public abstract class EntidadAuditable
{
    public int Id { get; set; }

    public DateTime CreadoEnUtc { get; set; }

    public string CreadoPor { get; set; } = string.Empty;

    public DateTime? ModificadoEnUtc { get; set; }

    public string? ModificadoPor { get; set; }

    /// <summary>Token de concurrencia: evita que dos usuarios se pisen cambios sin enterarse.</summary>
    [NoAuditar]
    public byte[] RowVersion { get; set; } = [];
}
