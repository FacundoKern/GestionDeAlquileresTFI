using System.ComponentModel.DataAnnotations;
using GestionAlquileres.Web.Security;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GestionAlquileres.Web.ViewModels.Usuarios;

public record UsuarioFilaViewModel(
    string Id,
    string NombreCompleto,
    string Email,
    string Rol,
    UsuarioEstadoViewModel Estado,
    bool EsUstedMismo);

/// <summary>Situación de la cuenta (no editable desde el formulario de datos).</summary>
public record UsuarioEstadoViewModel(
    bool Activo,
    DateTime? BloqueadoHastaUtc,
    bool DebeCambiarClave,
    DateTime CreadoEnUtc,
    DateTime? UltimoIngresoUtc)
{
    public bool Bloqueado => BloqueadoHastaUtc is not null;
}

/// <summary>Requisitos de contraseña de Identity (ver SeguridadExtensions), para mostrarlos en los formularios.</summary>
public static class RequisitosClave
{
    public const string Texto =
        "Al menos 12 caracteres, con mayúsculas, minúsculas, números y algún símbolo (por ejemplo ! # $ %).";
}

/// <summary>Contraseña temporal: el usuario la tiene que cambiar en su primer ingreso.</summary>
public class ClaveTemporalViewModel
{
    [Required(ErrorMessage = "Ingresá una contraseña temporal.")]
    [StringLength(128, MinimumLength = 12, ErrorMessage = "La contraseña debe tener al menos {2} caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña temporal")]
    public string Clave { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repetí la contraseña temporal.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Clave), ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Repetir contraseña temporal")]
    public string ConfirmarClave { get; set; } = string.Empty;
}

public class UsuarioCrearViewModel : ClaveTemporalViewModel
{
    [Required(ErrorMessage = "Ingresá el nombre completo.")]
    [StringLength(150, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá el email.")]
    [EmailAddress(ErrorMessage = "Ingresá un email válido.")]
    [StringLength(256, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Email (se usa para iniciar sesión)")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Elegí el rol.")]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = Roles.Operador;
}

/// <summary>Datos editables del usuario. El Id sale de la ruta; el estado se muestra pero no se bindea.</summary>
public class UsuarioEditarViewModel
{
    [BindNever]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá el nombre completo.")]
    [StringLength(150, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá el email.")]
    [EmailAddress(ErrorMessage = "Ingresá un email válido.")]
    [StringLength(256, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Email (se usa para iniciar sesión)")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Elegí el rol.")]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = string.Empty;

    /// <summary>Token de concurrencia de Identity.</summary>
    public string? ConcurrencyStamp { get; set; }

    [BindNever]
    public UsuarioEstadoViewModel? Estado { get; set; }

    /// <summary>El administrador que está editando su propia cuenta: no puede cambiarse el rol ni desactivarse.</summary>
    [BindNever]
    public bool EsUstedMismo { get; set; }
}

public class ResetearClaveViewModel : ClaveTemporalViewModel
{
    [BindNever]
    public string Id { get; set; } = string.Empty;

    [BindNever]
    public string NombreCompleto { get; set; } = string.Empty;
}
