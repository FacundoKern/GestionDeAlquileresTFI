using GestionAlquileres.Web.Models.Base;
using GestionAlquileres.Web.Models.Enums;

namespace GestionAlquileres.Web.Models.Entidades;

/// <summary>
/// Registro de auditoría. Solo se agregan filas. Contiene datos personales (valores antes/después),
/// por eso solo lo ve el rol Administrador.
/// </summary>
[NoAuditar]
public class AuditLog : ISoloAgregar
{
    public long Id { get; set; }

    public DateTime FechaUtc { get; set; }

    public string? UsuarioId { get; set; }

    public string? UsuarioNombre { get; set; }

    public AccionAuditoria Accion { get; set; }

    public string Entidad { get; set; } = string.Empty;

    public string? EntidadId { get; set; }

    /// <summary>JSON con los valores anteriores de las propiedades modificadas.</summary>
    public string? ValoresAnteriores { get; set; }

    /// <summary>JSON con los valores nuevos de las propiedades modificadas.</summary>
    public string? ValoresNuevos { get; set; }

    public string? DireccionIp { get; set; }

    public string? Detalle { get; set; }
}
