namespace GestionAlquileres.Web.Services.Comun;

/// <summary>
/// Qué datos sensibles puede ver el usuario actual. Lo arma el controlador a partir de las políticas y lo
/// reciben los servicios, que directamente no consultan ni envían lo que el usuario no puede ver.
/// </summary>
/// <param name="DatosPersonalesCompletos">Documento, teléfono y email completos (si no, parcialmente ocultos).</param>
/// <param name="Contratos">Información de contratos (si no, la sección no se muestra).</param>
public record AlcanceDatos(bool DatosPersonalesCompletos, bool Contratos);
