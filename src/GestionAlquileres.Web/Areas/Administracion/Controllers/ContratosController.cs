using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Services.Auditoria;
using GestionAlquileres.Web.Services.Comun;
using GestionAlquileres.Web.Services.Contratos;
using GestionAlquileres.Web.ViewModels.Contratos;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.Web.Areas.Administracion.Controllers;

/// <summary>
/// Contratos (ABM mínimo del TFI): listado, alta, ficha, rescisión y PDF adjuntos. Solo Administrador
/// (decisión del usuario: la ficha del contrato es solo para Administrador por ahora).
/// </summary>
public class ContratosController : AdministracionControllerBase
{
    /// <summary>Límite del pedido al subir un PDF: 10 MB más margen. El límite real lo controla el servicio.</summary>
    private const long LimiteSubida = 12L * 1024 * 1024;

    private readonly IContratosService _servicio;
    private readonly IAuditoriaService _auditoria;

    public ContratosController(IContratosService servicio, IAuditoriaService auditoria)
    {
        _servicio = servicio;
        _auditoria = auditoria;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] FiltroContratos filtro, CancellationToken ct) =>
        View(new ContratosIndexViewModel(filtro, await _servicio.ListarAsync(filtro, ct)));

    [HttpGet]
    public async Task<IActionResult> Detalle(int id, CancellationToken ct)
    {
        var detalle = await _servicio.ObtenerDetalleAsync(id, ct);
        if (detalle is null)
        {
            return NotFound();
        }

        // La ficha muestra nombres y documentos de inquilinos y garantes: su apertura se audita.
        await _auditoria.RegistrarConsultaDatosPersonalesAsync("Contrato", id, ct);
        return View(detalle);
    }

    /// <param name="propiedadId">Opcional: viene de la ficha de una propiedad y la deja elegida.</param>
    [HttpGet]
    public async Task<IActionResult> Crear(int? propiedadId, CancellationToken ct) =>
        View(await _servicio.NuevoFormularioAsync(propiedadId, ct));

    [HttpPost]
    public async Task<IActionResult> Crear(ContratoFormViewModel modelo, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var (resultado, advertencia) = await _servicio.CrearAsync(modelo, ct);
            if (resultado.Exito)
            {
                TempData[MensajesTempData.Exito] = "El contrato se creó correctamente."
                    + (advertencia is null ? string.Empty : $" Atención: {advertencia}");
                return RedirectToAction(nameof(Detalle), new { id = resultado.Id });
            }

            AgregarError(resultado);
        }

        await _servicio.PrepararFormularioAsync(modelo, ct);
        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Rescindir(int id, CancellationToken ct)
    {
        var modelo = await _servicio.ObtenerParaRescindirAsync(id, ct);
        return modelo is null ? NotFound() : View(modelo);
    }

    [HttpPost]
    public async Task<IActionResult> Rescindir(int id, RescindirContratoViewModel modelo, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var resultado = await _servicio.RescindirAsync(id, modelo, ct);
            if (resultado.Exito)
            {
                TempData[MensajesTempData.Exito] = "El contrato quedó rescindido.";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            AgregarError(resultado);
        }

        // Los datos de solo lectura (propiedad, vigencia) no vienen del formulario: se vuelven a leer.
        if (await _servicio.ObtenerParaRescindirAsync(id, ct) is not { } datos)
        {
            return NotFound();
        }

        modelo.Id = id;
        modelo.Propiedad = datos.Propiedad;
        modelo.FechaInicio = datos.FechaInicio;
        modelo.FechaFin = datos.FechaFin;
        return View(modelo);
    }

    /// <param name="id">El contrato.</param>
    [HttpPost]
    [RequestSizeLimit(LimiteSubida)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimiteSubida)]
    public async Task<IActionResult> SubirDocumento(int id, IFormFile? archivo, string? descripcion, CancellationToken ct)
    {
        var resultado = await _servicio.SubirDocumentoAsync(id, archivo, descripcion, ct);
        if (resultado == ResultadoOperacion.NoEncontrado())
        {
            return NotFound();
        }

        TempData[resultado.Exito ? MensajesTempData.Exito : MensajesTempData.Error] =
            resultado.Exito ? "El PDF se adjuntó al contrato." : resultado.Error;
        return RedirectToAction(nameof(Detalle), null, new { id }, "documentos");
    }

    [HttpGet]
    public async Task<IActionResult> Documento(int id, CancellationToken ct)
    {
        if (await _servicio.ObtenerDocumentoAsync(id, ct) is not { } documento)
        {
            return NotFound();
        }

        // Siempre como descarga (attachment): el PDF lo subió un usuario y no se abre dentro del sitio.
        return PhysicalFile(documento.Ruta, "application/pdf", documento.NombreDescarga);
    }

    [HttpGet]
    public async Task<IActionResult> EliminarDocumento(int id, CancellationToken ct)
    {
        var modelo = await _servicio.ObtenerParaEliminarDocumentoAsync(id, ct);
        return modelo is null ? NotFound() : View(modelo);
    }

    [HttpPost]
    [ActionName(nameof(EliminarDocumento))]
    public async Task<IActionResult> ConfirmarEliminarDocumento(int id, string? rowVersion, CancellationToken ct)
    {
        var (resultado, contratoId) = await _servicio.EliminarDocumentoAsync(id, rowVersion, ct);
        if (contratoId is null)
        {
            return NotFound();
        }

        TempData[resultado.Exito ? MensajesTempData.Exito : MensajesTempData.Error] =
            resultado.Exito ? "El documento se eliminó." : resultado.Error;
        return RedirectToAction(nameof(Detalle), null, new { id = contratoId }, "documentos");
    }

    private void AgregarError(ResultadoOperacion resultado) =>
        ModelState.AddModelError(resultado.Campo ?? string.Empty, resultado.Error ?? "No se pudo completar la operación.");
}
