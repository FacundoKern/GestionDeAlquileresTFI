namespace GestionAlquileres.Web.Models.Base;

/// <summary>Entidad de negocio: auditable y con borrado lógico.</summary>
public abstract class EntidadNegocio : EntidadAuditable, IBorradoLogico
{
    public bool Eliminado { get; set; }

    public DateTime? EliminadoEnUtc { get; set; }

    public string? EliminadoPor { get; set; }
}
