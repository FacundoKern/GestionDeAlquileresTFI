using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Services.Comun;
using GestionAlquileres.Web.Services.Usuarios;
using GestionAlquileres.Web.ViewModels.Usuarios;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.Web.Areas.Administracion.Controllers;

/// <summary>
/// ABM mínimo de usuarios: alta con contraseña temporal, datos y rol, activar/desactivar, desbloquear y
/// resetear la contraseña. No hay borrado: un usuario que ya no trabaja se desactiva (su historial en la
/// auditoría sigue apuntando a él). Las reglas están en <see cref="IUsuariosService"/>.
/// </summary>
public class UsuariosController : AdministracionControllerBase
{
    private readonly IUsuariosService _servicio;
    private readonly UserManager<Usuario> _userManager;
    private readonly SignInManager<Usuario> _signInManager;

    public UsuariosController(IUsuariosService servicio, UserManager<Usuario> userManager, SignInManager<Usuario> signInManager)
    {
        _servicio = servicio;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await _servicio.ListarAsync(ct));

    [HttpGet]
    public IActionResult Crear() => View(new UsuarioCrearViewModel());

    [HttpPost]
    public async Task<IActionResult> Crear(UsuarioCrearViewModel modelo, CancellationToken ct)
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

        TempData[MensajesTempData.Exito] =
            $"El usuario {modelo.Email.Trim()} se creó. Comunicale la contraseña temporal en persona: la va a tener que cambiar al ingresar.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(string id, CancellationToken ct)
    {
        var modelo = await _servicio.ObtenerParaEditarAsync(id, ct);
        return modelo is null ? NotFound() : View(modelo);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(string id, UsuarioEditarViewModel modelo, CancellationToken ct)
    {
        modelo.Id = id; // el Id sale de la ruta, nunca del formulario
        if (ModelState.IsValid)
        {
            var resultado = await _servicio.ActualizarAsync(id, modelo, ct);
            if (resultado.Exito)
            {
                await RefrescarSesionSiEsElMismoAsync(id);
                TempData[MensajesTempData.Exito] = "Los cambios se guardaron correctamente.";
                return RedirectToAction(nameof(Editar), new { id });
            }

            AgregarError(resultado);
        }

        await _servicio.CompletarEstadoAsync(modelo, ct);
        return modelo.Estado is null ? NotFound() : View(modelo);
    }

    [HttpPost]
    public Task<IActionResult> Activar(string id, CancellationToken ct) =>
        EjecutarAccionAsync(id, _servicio.CambiarActivoAsync(id, activo: true, ct), "La cuenta se activó.");

    [HttpPost]
    public Task<IActionResult> Desactivar(string id, CancellationToken ct) =>
        EjecutarAccionAsync(id, _servicio.CambiarActivoAsync(id, activo: false, ct),
            "La cuenta se desactivó. Si tenía una sesión abierta, se va a cerrar en unos minutos.");

    [HttpPost]
    public Task<IActionResult> Desbloquear(string id, CancellationToken ct) =>
        EjecutarAccionAsync(id, _servicio.DesbloquearAsync(id, ct), "La cuenta se desbloqueó: ya puede volver a intentar ingresar.");

    [HttpGet]
    public async Task<IActionResult> ResetearClave(string id, CancellationToken ct)
    {
        var nombre = await _servicio.ObtenerNombreAsync(id, ct);
        return nombre is null ? NotFound() : View(new ResetearClaveViewModel { Id = id, NombreCompleto = nombre });
    }

    [HttpPost]
    public async Task<IActionResult> ResetearClave(string id, ResetearClaveViewModel modelo, CancellationToken ct)
    {
        var nombre = await _servicio.ObtenerNombreAsync(id, ct);
        if (nombre is null)
        {
            return NotFound();
        }

        modelo.Id = id;
        modelo.NombreCompleto = nombre;
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var resultado = await _servicio.ResetearClaveAsync(id, modelo.Clave, ct);
        if (!resultado.Exito)
        {
            AgregarError(resultado);
            return View(modelo);
        }

        TempData[MensajesTempData.Exito] =
            "La contraseña se reseteó. Comunicale la contraseña temporal en persona: la va a tener que cambiar al ingresar.";
        return RedirectToAction(nameof(Editar), new { id });
    }

    private async Task<IActionResult> EjecutarAccionAsync(string id, Task<ResultadoOperacion> accion, string mensajeExito)
    {
        var resultado = await accion;
        TempData[resultado.Exito ? MensajesTempData.Exito : MensajesTempData.Error] = resultado.Exito ? mensajeExito : resultado.Error;
        return RedirectToAction(nameof(Editar), new { id });
    }

    private void AgregarError(ResultadoOperacion resultado) =>
        ModelState.AddModelError(resultado.Campo ?? string.Empty, resultado.Error ?? "No se pudo completar la operación.");

    /// <summary>Si el administrador cambió sus propios datos, actualiza su sesión (nombre visible, email).</summary>
    private async Task RefrescarSesionSiEsElMismoAsync(string id)
    {
        if (id == _userManager.GetUserId(User) && await _userManager.FindByIdAsync(id) is { } usuario)
        {
            await _signInManager.RefreshSignInAsync(usuario);
        }
    }
}
