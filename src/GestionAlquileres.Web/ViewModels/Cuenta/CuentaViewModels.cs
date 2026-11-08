using System.ComponentModel.DataAnnotations;

namespace GestionAlquileres.Web.ViewModels.Cuenta;

public class IniciarSesionViewModel
{
    [Required(ErrorMessage = "Ingresá tu email.")]
    [EmailAddress(ErrorMessage = "Ingresá un email válido.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Clave { get; set; } = string.Empty;

    [Display(Name = "Recordarme durante 7 días")]
    public bool Recordarme { get; set; }

    public string? ReturnUrl { get; set; }
}

public class CambiarClaveViewModel
{
    [Required(ErrorMessage = "Ingresá tu contraseña actual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña actual")]
    public string ClaveActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá la nueva contraseña.")]
    [StringLength(128, MinimumLength = 12, ErrorMessage = "La nueva contraseña debe tener al menos {2} caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string ClaveNueva { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repetí la nueva contraseña.")]
    [DataType(DataType.Password)]
    [Compare(nameof(ClaveNueva), ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Repetir nueva contraseña")]
    public string ConfirmarClave { get; set; } = string.Empty;

    public bool EsObligatorio { get; set; }
}
