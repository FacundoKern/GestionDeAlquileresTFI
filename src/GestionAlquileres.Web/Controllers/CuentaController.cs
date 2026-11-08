using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Infrastructure.Configuracion;
using GestionAlquileres.Web.Services.Usuarios;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Models.Enums;
using GestionAlquileres.Web.Security;
using GestionAlquileres.Web.Services.Auditoria;
using GestionAlquileres.Web.ViewModels.Cuenta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GestionAlquileres.Web.Controllers;

/// <summary>Inicio y cierre de sesión y cambio de contraseña. No hay registro público.</summary>
public class CuentaController : Controller
{
    private const string MensajeCredencialesInvalidas = "El email o la contraseña no son correctos.";

    private readonly SignInManager<Usuario> _signInManager;
    private readonly UserManager<Usuario> _userManager;
    private readonly IAuditoriaService _auditoria;
    private readonly IUsuariosService _usuarios;
    private readonly ILogger<CuentaController> _logger;

    public CuentaController(
        SignInManager<Usuario> signInManager,
        UserManager<Usuario> userManager,
        IAuditoriaService auditoria,
        IUsuariosService usuarios,
        ILogger<CuentaController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditoria = auditoria;
        _usuarios = usuarios;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult IniciarSesion(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(InicioController.Index), "Inicio");
        }

        return View(new IniciarSesionViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(SeguridadExtensions.PoliticaRateLimitLogin)]
    public async Task<IActionResult> IniciarSesion(IniciarSesionViewModel modelo, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var usuario = await _userManager.FindByEmailAsync(modelo.Email);
        if (usuario is null || !usuario.Activo)
        {
            await RegistrarIntentoFallidoAsync(modelo.Email, usuario is null ? "Usuario inexistente" : "Usuario inactivo", ct);
            ModelState.AddModelError(string.Empty, MensajeCredencialesInvalidas);
            return View(modelo);
        }

        // Recordarme = cookie persistente que dura 7 días (ver SeguridadExtensions); si no, dura la sesión del navegador.
        var resultado = await _signInManager.PasswordSignInAsync(usuario, modelo.Clave, isPersistent: modelo.Recordarme, lockoutOnFailure: true);

        if (resultado.Succeeded)
        {
            await _usuarios.RegistrarIngresoAsync(usuario.Id, ct);
            await _auditoria.RegistrarEventoAsync(AccionAuditoria.InicioSesion, nameof(Usuario), usuario.Id,
                modelo.Recordarme ? $"Sesión recordada ({SeguridadExtensions.DuracionSesionRecordada.TotalDays:0} días)" : null, ct);
            _logger.LogInformation("Inicio de sesión correcto del usuario {UsuarioId}", usuario.Id);

            return Url.IsLocalUrl(modelo.ReturnUrl)
                ? LocalRedirect(modelo.ReturnUrl)
                : RedirectToAction(nameof(InicioController.Index), "Inicio");
        }

        if (resultado.IsLockedOut)
        {
            await _auditoria.RegistrarEventoAsync(AccionAuditoria.CuentaBloqueada, nameof(Usuario), usuario.Id,
                "Bloqueo por intentos fallidos", ct);
            _logger.LogWarning("Cuenta bloqueada por intentos fallidos: usuario {UsuarioId}", usuario.Id);
            ModelState.AddModelError(string.Empty,
                "Tu cuenta quedó bloqueada temporalmente por varios intentos fallidos. " +
                "Volvé a intentar en 15 minutos o pedile al administrador que la desbloquee.");
            return View(modelo);
        }

        await RegistrarIntentoFallidoAsync(modelo.Email, "Contraseña incorrecta", ct);
        ModelState.AddModelError(string.Empty, MensajeCredencialesInvalidas);
        return View(modelo);
    }

    [HttpPost]
    public async Task<IActionResult> CerrarSesion(CancellationToken ct)
    {
        await _auditoria.RegistrarEventoAsync(AccionAuditoria.CierreSesion, nameof(Usuario), _userManager.GetUserId(User), ct: ct);
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(IniciarSesion));
    }

    [HttpGet]
    public IActionResult CambiarClave() =>
        View(new CambiarClaveViewModel { EsObligatorio = User.HasClaim(ClaimsPropios.DebeCambiarClave, "true") });

    [HttpPost]
    public async Task<IActionResult> CambiarClave(CambiarClaveViewModel modelo, CancellationToken ct)
    {
        modelo.EsObligatorio = User.HasClaim(ClaimsPropios.DebeCambiarClave, "true");
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var usuario = await _userManager.GetUserAsync(User);
        if (usuario is null)
        {
            return RedirectToAction(nameof(IniciarSesion));
        }

        var resultado = await _userManager.ChangePasswordAsync(usuario, modelo.ClaveActual, modelo.ClaveNueva);
        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(modelo);
        }

        usuario.DebeCambiarClave = false;
        await _userManager.UpdateAsync(usuario);
        await _signInManager.RefreshSignInAsync(usuario);
        await _auditoria.RegistrarEventoAsync(AccionAuditoria.CambioClave, nameof(Usuario), usuario.Id, ct: ct);

        TempData[MensajesTempData.Exito] = "Tu contraseña se cambió correctamente.";
        return RedirectToAction(nameof(InicioController.Index), "Inicio");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccesoDenegado() => View();

    private Task RegistrarIntentoFallidoAsync(string email, string motivo, CancellationToken ct)
    {
        // El email intentado queda solo en el AuditLog (acceso restringido), no en los logs de la aplicación.
        _logger.LogWarning("Intento de inicio de sesión fallido");
        var emailRecortado = email.Length > 256 ? email[..256] : email;
        return _auditoria.RegistrarEventoAsync(AccionAuditoria.InicioSesionFallido, nameof(Usuario), null,
            $"{motivo}. Email ingresado: {emailRecortado}", ct);
    }
}
