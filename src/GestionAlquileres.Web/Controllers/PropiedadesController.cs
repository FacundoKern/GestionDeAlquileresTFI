using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Security;
using GestionAlquileres.Web.Services.Fotos;
using GestionAlquileres.Web.Services.Propiedades;
using GestionAlquileres.Web.ViewModels.Propiedades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.Web.Controllers;

/// <summary>
/// ABM de propiedades (con copropietarios). Ver: todos los roles. Crear, editar y eliminar: Administrador y Operador.
/// </summary>
[Authorize(Policy = Politicas.Lectura)]
public class PropiedadesController : AppControllerBase
{
    private readonly IPropiedadesService _servicio;
    private readonly IFotosPropiedadesService _fotos;

    public PropiedadesController(IPropiedadesService servicio, IFotosPropiedadesService fotos)
    {
        _servicio = servicio;
        _fotos = fotos;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] FiltroPropiedades filtro, CancellationToken ct) =>
        View(new PropiedadesIndexViewModel(filtro, await _servicio.ListarAsync(filtro, ct)));

    [HttpGet]
    public async Task<IActionResult> Detalle(int id, CancellationToken ct)
    {
        var detalle = await _servicio.ObtenerDetalleAsync(id, await ObtenerAlcanceDatosAsync(), ct);
        if (detalle is null)
        {
            return NotFound();
        }

        return View(detalle with { Galeria = await _fotos.ObtenerGaleriaAsync(id, detalle.Direccion, ct) });
    }

    /// <param name="propietarioId">Opcional: viene de la ficha de un propietario y lo deja elegido.</param>
    [HttpGet]
    [Authorize(Policy = Politicas.Gestion)]
    public async Task<IActionResult> Crear(int? propietarioId, CancellationToken ct) =>
        View(await _servicio.NuevoFormularioAsync(propietarioId, ct));

    [HttpPost]
    [Authorize(Policy = Politicas.Gestion)]
    public async Task<IActionResult> Crear(PropiedadFormViewModel modelo, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var resultado = await _servicio.CrearAsync(modelo, ct);
            if (resultado.Exito)
            {
                TempData[MensajesTempData.Exito] = "La propiedad se creó correctamente.";
                return RedirectToAction(nameof(Detalle), new { id = resultado.Id });
            }

            AgregarError(resultado);
        }

        await _servicio.PrepararFormularioAsync(modelo, ct);
        return View(modelo);
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
    public async Task<IActionResult> Editar(int id, PropiedadFormViewModel modelo, CancellationToken ct)
    {
        modelo.Id = id; // el Id sale de la ruta, nunca del formulario
        if (ModelState.IsValid)
        {
            var resultado = await _servicio.ActualizarAsync(id, modelo, ct);
            if (resultado.Exito)
            {
                TempData[MensajesTempData.Exito] = "Los cambios se guardaron correctamente.";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            AgregarError(resultado);
        }

        await _servicio.PrepararFormularioAsync(modelo, ct);
        return View(modelo);
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

        TempData[MensajesTempData.Exito] = "La propiedad se eliminó.";
        return RedirectToAction(nameof(Index));
    }
}
