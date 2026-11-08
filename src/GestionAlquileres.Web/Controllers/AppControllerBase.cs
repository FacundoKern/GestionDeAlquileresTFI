using GestionAlquileres.Web.Security;
using GestionAlquileres.Web.Services.Comun;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.Web.Controllers;

/// <summary>Ayudantes comunes de los controladores de ABM.</summary>
public abstract class AppControllerBase : Controller
{
    /// <summary>
    /// Qué datos sensibles puede ver el usuario: Administrador y Operador ven todo; Consulta ve documento,
    /// teléfono y email parcialmente ocultos y no ve información de contratos.
    /// </summary>
    protected async Task<AlcanceDatos> ObtenerAlcanceDatosAsync()
    {
        var autorizacion = HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();
        var datosCompletos = (await autorizacion.AuthorizeAsync(User, Politicas.DatosPersonalesCompletos)).Succeeded;
        var contratos = (await autorizacion.AuthorizeAsync(User, Politicas.InformacionContratos)).Succeeded;
        return new AlcanceDatos(datosCompletos, contratos);
    }

    /// <summary>Muestra el error del servicio junto al campo correspondiente (o en el resumen si es general).</summary>
    protected void AgregarError(ResultadoOperacion resultado) =>
        ModelState.AddModelError(resultado.Campo ?? string.Empty, resultado.Error ?? "No se pudo completar la operación.");
}
