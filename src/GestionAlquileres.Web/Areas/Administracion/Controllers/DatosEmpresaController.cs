using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Services.Empresa;
using GestionAlquileres.Web.ViewModels.Empresa;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.Web.Areas.Administracion.Controllers;

/// <summary>Edición de los datos de la inmobiliaria (pie de página, inicio y PDFs).</summary>
public class DatosEmpresaController : AdministracionControllerBase
{
    private readonly IDatosEmpresaService _servicio;

    public DatosEmpresaController(IDatosEmpresaService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(await _servicio.ObtenerParaEditarAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Index(DatosEmpresaFormViewModel modelo, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var resultado = await _servicio.GuardarAsync(modelo, ct);
        if (!resultado.Exito)
        {
            ModelState.AddModelError(resultado.Campo ?? string.Empty, resultado.Error!);
            return View(modelo);
        }

        TempData[MensajesTempData.Exito] = "Los datos de la inmobiliaria se guardaron correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
