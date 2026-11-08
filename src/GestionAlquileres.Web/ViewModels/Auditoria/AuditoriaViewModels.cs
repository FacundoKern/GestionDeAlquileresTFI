using GestionAlquileres.Web.Models.Enums;
using GestionAlquileres.Web.ViewModels.Comun;

namespace GestionAlquileres.Web.ViewModels.Auditoria;

/// <summary>Filtros del visor (llegan por query string). Las fechas son días en hora argentina.</summary>
public class FiltroAuditoria
{
    public DateOnly? Desde { get; set; }

    public DateOnly? Hasta { get; set; }

    /// <summary>Email (o parte) de quien hizo la operación.</summary>
    public string? Usuario { get; set; }

    public AccionAuditoria? Accion { get; set; }

    /// <summary>Tipo de registro (nombre de la entidad: Propietario, Inquilino...).</summary>
    public string? Entidad { get; set; }

    /// <summary>Id del registro, para ver el historial completo de uno solo.</summary>
    public string? EntidadId { get; set; }

    public int Pagina { get; set; } = 1;
}

public record AuditoriaIndexViewModel(
    FiltroAuditoria Filtro,
    ListaPaginada<RegistroAuditoriaViewModel> Resultados,
    IReadOnlyList<OpcionEntidadViewModel> Entidades);

public record OpcionEntidadViewModel(string Valor, string Nombre);

/// <param name="Registro">Texto para identificar el registro (p. ej. "Inquilino 12" o el nombre de un usuario).</param>
/// <param name="Enlace">Controlador y área de la ficha del registro, si existe y se puede abrir.</param>
public record RegistroAuditoriaViewModel(
    long Id,
    DateTime FechaUtc,
    string Usuario,
    AccionAuditoria Accion,
    string Entidad,
    string EntidadNombre,
    string? EntidadId,
    string Registro,
    EnlaceFicha? Enlace,
    string? Detalle,
    string? DireccionIp,
    IReadOnlyList<CambioCampoViewModel> Cambios);

public record EnlaceFicha(string Controlador, string Accion, string Area, string Id);

public record CambioCampoViewModel(string Campo, string? Antes, string? Despues);
