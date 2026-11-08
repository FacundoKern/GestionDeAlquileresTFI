using GestionAlquileres.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GestionAlquileres.Web.Security;

/// <summary>
/// Si el usuario tiene que cambiar la contraseña (alta o reseteo por el administrador), solo puede
/// usar las acciones de su cuenta hasta hacerlo.
/// </summary>
public sealed class CambioClaveObligatorioFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var usuario = context.HttpContext.User;
        if (usuario.Identity?.IsAuthenticated != true
            || !usuario.HasClaim(ClaimsPropios.DebeCambiarClave, "true")
            || context.Controller is CuentaController
            || context.Controller is InicioController && context.ActionDescriptor.RouteValues["action"] is "Error" or "Estado")
        {
            return;
        }

        context.Result = new RedirectToActionResult(nameof(CuentaController.CambiarClave), "Cuenta", new { area = string.Empty });
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
