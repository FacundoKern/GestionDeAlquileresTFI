using System.ComponentModel.DataAnnotations;

namespace GestionAlquileres.Web.Models.Enums;

public enum AccionAuditoria
{
    [Display(Name = "Creación")] Crear = 1,
    [Display(Name = "Modificación")] Modificar = 2,
    [Display(Name = "Eliminación")] Eliminar = 3,
    [Display(Name = "Restauración")] Restaurar = 4,
    [Display(Name = "Anulación")] Anular = 5,
    [Display(Name = "Consulta de datos personales")] ConsultaDatosPersonales = 10,
    [Display(Name = "Inicio de sesión")] InicioSesion = 20,
    [Display(Name = "Inicio de sesión fallido")] InicioSesionFallido = 21,
    [Display(Name = "Cierre de sesión")] CierreSesion = 22,
    [Display(Name = "Cuenta bloqueada")] CuentaBloqueada = 23,
    [Display(Name = "Cambio de contraseña")] CambioClave = 24,
    [Display(Name = "Gestión de usuario")] GestionUsuario = 30,
}
