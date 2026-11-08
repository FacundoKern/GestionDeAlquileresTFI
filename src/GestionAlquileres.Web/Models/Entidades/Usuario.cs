using GestionAlquileres.Web.Models.Base;
using Microsoft.AspNetCore.Identity;

namespace GestionAlquileres.Web.Models.Entidades;

/// <summary>
/// Usuario del sistema (ASP.NET Core Identity). No se audita automáticamente porque contiene el hash de
/// la contraseña y sellos de seguridad: la gestión de usuarios registra eventos explícitos en el AuditLog.
/// </summary>
[NoAuditar]
public class Usuario : IdentityUser
{
    public string NombreCompleto { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    /// <summary>Obliga a cambiar la contraseña en el próximo ingreso (alta o reseteo por el administrador).</summary>
    public bool DebeCambiarClave { get; set; }

    public DateTime CreadoEnUtc { get; set; }

    public DateTime? UltimoIngresoUtc { get; set; }
}
