using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Security;
using GestionAlquileres.Web.Services.Fotos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace GestionAlquileres.Web.Controllers;

/// <summary>
/// Fotos de las propiedades. Ver: todos los roles (las fotos no son datos personales). Subir, describir, elegir la
/// principal y eliminar: Administrador y Operador. Los archivos se sirven solo desde acá, con sesión iniciada:
/// no están en wwwroot.
/// </summary>
[Authorize(Policy = Politicas.Lectura)]
public class FotosPropiedadesController : AppControllerBase
{
    /// <summary>
    /// Límite del pedido completo al subir: 10 fotos de hasta 10 MB más margen. El límite real de cada archivo lo
    /// controla el servicio, con la configuración de la sección "Fotos". (Los atributos necesitan una constante.)
    /// </summary>
    private const long LimiteSubida = 120L * 1024 * 1024;

    private readonly IFotosPropiedadesService _servicio;

    public FotosPropiedadesController(IFotosPropiedadesService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> Ver(int id, bool miniatura, CancellationToken ct)
    {
        var ruta = await _servicio.ObtenerRutaAsync(id, miniatura, ct);
        if (ruta is null)
        {
            return NotFound();
        }

        // Cada foto tiene un Id propio y no cambia: se puede guardar en la caché del navegador (solo la privada).
        Response.Headers[HeaderNames.CacheControl] = "private, max-age=31536000, immutable";
        return PhysicalFile(ruta, "image/jpeg");
    }

    /// <param name="id">La propiedad.</param>
    [HttpPost]
    [Authorize(Policy = Politicas.Gestion)]
    [RequestSizeLimit(LimiteSubida)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimiteSubida)]
    public async Task<IActionResult> Subir(int id, List<IFormFile> archivos, CancellationToken ct)
    {
        var resultado = await _servicio.SubirAsync(id, archivos, ct);
        if (resultado is null)
        {
            return NotFound();
        }

        if (resultado.Subidas > 0)
        {
            TempData[MensajesTempData.Exito] = resultado.Subidas == 1 ? "Se agregó 1 foto." : $"Se agregaron {resultado.Subidas} fotos.";
        }

        if (resultado.Errores.Count > 0)
        {
            TempData[MensajesTempData.Error] = string.Join(" ", resultado.Errores);
        }

        return VolverALaPropiedad(id);
    }

    [HttpPost]
    [Authorize(Policy = Politicas.Gestion)]
    public async Task<IActionResult> Descripcion(int id, string? descripcion, string? rowVersion, CancellationToken ct) =>
        Resultado(await _servicio.ActualizarDescripcionAsync(id, descripcion, rowVersion, ct), "La descripción se guardó.");

    [HttpPost]
    [Authorize(Policy = Politicas.Gestion)]
    public async Task<IActionResult> Principal(int id, CancellationToken ct) =>
        Resultado(await _servicio.MarcarPrincipalAsync(id, ct), "La foto quedó como principal.");

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
    public async Task<IActionResult> ConfirmarEliminar(int id, string? rowVersion, CancellationToken ct) =>
        Resultado(await _servicio.EliminarAsync(id, rowVersion, ct), "La foto se eliminó.");

    private IActionResult Resultado(ResultadoAccionFoto? accion, string mensajeExito)
    {
        if (accion is null)
        {
            return NotFound(); // la foto no existe o ya se eliminó
        }

        var resultado = accion.Resultado;
        TempData[resultado.Exito ? MensajesTempData.Exito : MensajesTempData.Error] = resultado.Exito ? mensajeExito : resultado.Error;
        return VolverALaPropiedad(accion.PropiedadId);
    }

    private RedirectToActionResult VolverALaPropiedad(int propiedadId) =>
        RedirectToAction(nameof(PropiedadesController.Detalle), "Propiedades", new { id = propiedadId }, "fotos");
}
