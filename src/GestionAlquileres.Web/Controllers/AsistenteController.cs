using GestionAlquileres.Web.Infrastructure.Configuracion;
using GestionAlquileres.Web.Security;
using GestionAlquileres.Web.Services.Asistente;
using GestionAlquileres.Web.ViewModels.Asistente;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GestionAlquileres.Web.Controllers;

/// <summary>
/// Asistente de ayuda con IA (Gemini), para todos los roles. Sin JavaScript es una página con un formulario; con
/// JavaScript, el panel flotante del layout manda la misma pregunta por AJAX y recibe la respuesta en JSON.
/// </summary>
[Authorize(Policy = Politicas.Lectura)]
public class AsistenteController : Controller
{
    private readonly IAsistenteService _asistente;

    public AsistenteController(IAsistenteService asistente)
    {
        _asistente = asistente;
    }

    [HttpGet]
    public IActionResult Index() => View(new AsistenteViewModel());

    [HttpPost]
    [EnableRateLimiting(AsistenteExtensions.PoliticaRateLimitAsistente)]
    public async Task<IActionResult> Preguntar(string? pregunta, CancellationToken ct)
    {
        // Solo el rol (no es un dato personal): sirve para explicar qué puede hacer cada usuario.
        var rol = Roles.Todos.FirstOrDefault(User.IsInRole);
        var respuesta = await _asistente.ResponderAsync(pregunta, rol, ct);

        if (Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            return Json(new { exito = respuesta.Exito, texto = respuesta.Texto });
        }

        return View(nameof(Index), new AsistenteViewModel { Pregunta = pregunta, Respuesta = respuesta });
    }
}
