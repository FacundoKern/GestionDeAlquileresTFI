using GestionAlquileres.Web.Services.Auditoria;
using GestionAlquileres.Web.ViewModels.Auditoria;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.Web.Areas.Administracion.Controllers;

/// <summary>Visor del registro de auditoría (solo lectura: el registro no se puede modificar ni borrar).</summary>
public class AuditoriaController : AdministracionControllerBase
{
    private readonly IVisorAuditoriaService _servicio;

    public AuditoriaController(IVisorAuditoriaService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] FiltroAuditoria filtro, CancellationToken ct) =>
        View(await _servicio.ListarAsync(filtro, ct));
}
