using System.ComponentModel.DataAnnotations;
using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Models.Enums;

namespace GestionAlquileres.Web.ViewModels.Personas;

/// <summary>
/// Formulario de datos personales común a Propietario, Inquilino y Garante. Solo expone los campos
/// editables (evita overposting): los campos de auditoría y de borrado lógico nunca se bindean.
/// </summary>
public abstract class PersonaFormViewModel : IValidatableObject
{
    /// <summary>0 = alta. En edición se toma de la ruta, no del formulario.</summary>
    public int Id { get; set; }

    [Display(Name = "Tipo de persona")]
    public TipoPersona TipoPersona { get; set; } = TipoPersona.Fisica;

    [Required(ErrorMessage = "Ingresá el nombre o la razón social.")]
    [StringLength(150, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Nombre o razón social")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Apellido")]
    public string? Apellido { get; set; }

    [Display(Name = "Tipo de documento")]
    public TipoDocumento TipoDocumento { get; set; } = TipoDocumento.Dni;

    [Required(ErrorMessage = "Ingresá el número de documento.")]
    [StringLength(20, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Número de documento")]
    public string NumeroDocumento { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Ingresá un email válido.")]
    [StringLength(256, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(50, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [StringLength(250, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Domicilio")]
    public string? Domicilio { get; set; }

    [StringLength(2000, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Notas")]
    public string? Notas { get; set; }

    /// <summary>Token de concurrencia (base64).</summary>
    public string? RowVersion { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TipoPersona == TipoPersona.Fisica && string.IsNullOrWhiteSpace(Apellido))
        {
            yield return new ValidationResult("Ingresá el apellido.", [nameof(Apellido)]);
        }

        if (TipoPersona == TipoPersona.Juridica && TipoDocumento != TipoDocumento.Cuit)
        {
            yield return new ValidationResult("Una persona jurídica se identifica con CUIT.", [nameof(TipoDocumento)]);
        }

        if (Documentos.Validar(TipoDocumento, NumeroDocumento) is { } error && !string.IsNullOrWhiteSpace(NumeroDocumento))
        {
            yield return new ValidationResult(error, [nameof(NumeroDocumento)]);
        }
    }
}

/// <summary>
/// Datos personales para mostrar en una ficha. Documento, teléfono y email ya vienen completos o
/// parcialmente ocultos según el rol (<see cref="DatosEnmascarados"/>).
/// </summary>
public record PersonaDatosViewModel(
    string NombreCompleto,
    TipoPersona TipoPersona,
    string Documento,
    bool DatosEnmascarados,
    string? Email,
    string? Telefono,
    string? Domicilio,
    string? Notas,
    string CreadoPor,
    DateTime CreadoEnUtc,
    string? ModificadoPor,
    DateTime? ModificadoEnUtc);

/// <summary>Datos para la página de confirmación de borrado.</summary>
public record EliminarPersonaViewModel(int Id, string NombreCompleto, string Documento, string RowVersion, string? MotivoNoSePuede);
