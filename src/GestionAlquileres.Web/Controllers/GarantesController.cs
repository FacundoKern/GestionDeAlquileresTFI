using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Security;
using GestionAlquileres.Web.Services.Auditoria;
using GestionAlquileres.Web.Services.Garantes;
using GestionAlquileres.Web.ViewModels.Garantes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.Web.Controllers;

/// <summary>
/// ABM de garantes. Ver: todos los roles (Consulta sin información de contratos). Crear, editar y
/// eliminar: Administrador y Operador. La lógica está en <see cref="IGarantesService"/>.
/// </summary>
[Authorize(Policy = Politicas.Lectura)]
public class GarantesController : AppControllerBase
{
    private readonly IGarantesService _servicio;
    private readonly IAuditoriaService _auditoria;

    public GarantesController(IGarantesService servicio, IAuditoriaService auditoria)
    {
        _servicio = servicio;
        _auditoria = auditoria;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] FiltroGarantes filtro, CancellationToken ct)
    {
        var alcance = await ObtenerAlcanceDatosAsync();
        var resultados = await _servicio.ListarAsync(filtro, alcance, ct);
        return View(new GarantesIndexViewModel(filtro, resultados, alcance.Contratos));
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id, CancellationToken ct)
    {
        var detalle = await _servicio.ObtenerDetalleAsync(id, await ObtenerAlcanceDatosAsync(), ct);
        if (detalle is null)
        {
            return NotFound();
        }

        // La ficha contiene datos personales: su apertura queda auditada.
        await _auditoria.RegistrarConsultaDatosPersonalesAsync(nameof(Garante), id, ct);
        return View(detalle);
    }

    [HttpGet]
    [Authorize(Policy = Politicas.Gestion)]
    public IActionResult Crear() => View(new GaranteFormViewModel());

    [HttpPost]
    [Authorize(Policy = Politicas.Gestion)]
    public async Task<IActionResult> Crear(GaranteFormViewModel modelo, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var resultado = await _servicio.CrearAsync(modelo, ct);
        if (!resultado.Exito)
        {
            AgregarError(resultado);
            return View(modelo);
        }

        TempData[MensajesTempData.Exito] = "El garante se creó correctamente.";
        return RedirectToAction(nameof(Detalle), new { id = resultado.Id });
    }

    [HttpGet]
    [Authorize(Policy = Politicas.Gestion)]
    public async Task<IActionResult> Editar(int id, CancellationToken ct)
    {
        var modelo = await _servicio.ObtenerParaEditarAsync(id, ct);
        return modelo is null ? NotFound() : View(modelo);
    }

    [HttpPost]
    [Authorize(Policy = Politicas.Gestion)]
    public async Task<IActionResult> Editar(int id, GaranteFormViewModel modelo, CancellationToken ct)
    {
        modelo.Id = id; // el Id sale de la ruta, nunca del formulario
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var resultado = await _servicio.ActualizarAsync(id, modelo, ct);
        if (!resultado.Exito)
        {
            AgregarError(resultado);
            return View(modelo);
        }

        TempData[MensajesTempData.Exito] = "Los cambios se guardaron correctamente.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpGet]
    [Authorize(Policy = Politicas.Gestion)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        var modelo = await _servicio.ObtenerParaEliminarAsync(id, ct);
        return modelo is null ? NotFound() : View(modelo);
    }

    [HttpPost]
    [Authorize(Policy = Politicas.Gestion)]
    [ActionName(nameof(Eliminar))]
    public async Task<IActionResult> ConfirmarEliminar(int id, string? rowVersion, CancellationToken ct)
    {
        var resultado = await _servicio.EliminarAsync(id, rowVersion, ct);
        if (!resultado.Exito)
        {
            TempData[MensajesTempData.Error] = resultado.Error;
            return RedirectToAction(nameof(Detalle), new { id });
        }

        TempData[MensajesTempData.Exito] = "El garante se eliminó.";
        return RedirectToAction(nameof(Index));
    }
}
