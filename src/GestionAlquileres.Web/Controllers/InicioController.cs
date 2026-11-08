using System.Diagnostics;
using GestionAlquileres.Web.Security;
using GestionAlquileres.Web.Services.Inicio;
using GestionAlquileres.Web.ViewModels.Comun;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.Web.Controllers;

[Authorize(Policy = Politicas.Lectura)]
public class InicioController : AppControllerBase
{
    private readonly ITableroService _tablero;

    public InicioController(ITableroService tablero)
    {
        _tablero = tablero;
    }

    /// <summary>El tablero muestra información de contratos: para roles sin ese permiso (Consulta) ni se calcula.</summary>
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var alcance = await ObtenerAlcanceDatosAsync();
        return View(alcance.Contratos ? await _tablero.ObtenerAsync(ct) : null);
    }

    /// <summary>Página de error genérica: nunca muestra detalles internos, solo un identificador para soporte.</summary>
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { IdSolicitud = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Estado(int id) => View(ErrorViewModel.ParaCodigo(id));
}
