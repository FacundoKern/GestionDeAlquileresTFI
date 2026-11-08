using System.ComponentModel.DataAnnotations;
using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Models.Enums;

namespace GestionAlquileres.Web.ViewModels.Empresa;

/// <summary>Datos de la inmobiliaria para mostrar (pie de página, inicio, PDFs).</summary>
public record DatosEmpresaViewModel
{
    public required string NombreComercial { get; init; }
    public string? RazonSocial { get; init; }
    public string? Cuit { get; init; }
    public string? MatriculaCorredor { get; init; }
    public string? Direccion { get; init; }
    public string? Localidad { get; init; }
    public string? Provincia { get; init; }
    public string? CodigoPostal { get; init; }
    public string? Telefono { get; init; }
    public string? Whatsapp { get; init; }
    public string? Email { get; init; }
    public string? SitioWeb { get; init; }
    public string? HorarioAtencion { get; init; }
    public string? Descripcion { get; init; }
    public string? SoporteNombre { get; init; }
    public string? SoporteTelefono { get; init; }
    public string? SoporteEmail { get; init; }

    public string? CuitFormateado => string.IsNullOrEmpty(Cuit) ? null : Documentos.Formatear(TipoDocumento.Cuit, Cuit);

    /// <summary>"Calle 123, Localidad, Provincia (CP)" con las partes que estén cargadas.</summary>
    public string? DireccionCompleta
    {
        get
        {
            var partes = new[] { Direccion, Localidad, Provincia }.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
            if (partes.Count == 0)
            {
                return null;
            }

            var texto = string.Join(", ", partes);
            return string.IsNullOrWhiteSpace(CodigoPostal) ? texto : $"{texto} ({CodigoPostal})";
        }
    }

    /// <summary>URL del sitio solo si es http/https (nunca se arma un enlace con otro esquema, p. ej. javascript:).</summary>
    public string? SitioWebEnlace =>
        Uri.TryCreate(SitioWeb, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            ? uri.ToString()
            : null;

    public bool TieneSoporte => !string.IsNullOrWhiteSpace(SoporteTelefono) || !string.IsNullOrWhiteSpace(SoporteEmail);

    /// <summary>Enlace tel: con solo dígitos y el signo +.</summary>
    public static string EnlaceTelefono(string telefono) =>
        "tel:" + new string(telefono.Where(c => char.IsDigit(c) || c == '+').ToArray());

    /// <summary>
    /// Enlace de WhatsApp (wa.me). Si el número no trae código de país, se asume Argentina (+54 9).
    /// </summary>
    public static string EnlaceWhatsapp(string numero)
    {
        var digitos = new string(numero.Where(char.IsDigit).ToArray());
        if (!digitos.StartsWith("54", StringComparison.Ordinal))
        {
            digitos = "549" + digitos.TrimStart('0');
        }

        return "https://wa.me/" + digitos;
    }
}

/// <summary>Formulario de edición. Solo expone los campos editables (evita overposting).</summary>
public class DatosEmpresaFormViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Ingresá el nombre comercial.")]
    [StringLength(150, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Nombre comercial")]
    public string NombreComercial { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Razón social")]
    public string? RazonSocial { get; set; }

    [Display(Name = "CUIT")]
    public string? Cuit { get; set; }

    [StringLength(50, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Matrícula del corredor inmobiliario")]
    public string? MatriculaCorredor { get; set; }

    [StringLength(200, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Dirección")]
    public string? Direccion { get; set; }

    [StringLength(100, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Localidad")]
    public string? Localidad { get; set; }

    [StringLength(60, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Provincia")]
    public string? Provincia { get; set; }

    [StringLength(10, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Código postal")]
    public string? CodigoPostal { get; set; }

    [StringLength(50, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [StringLength(50, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "WhatsApp")]
    public string? Whatsapp { get; set; }

    [EmailAddress(ErrorMessage = "Ingresá un email válido.")]
    [StringLength(256, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Url(ErrorMessage = "Ingresá una dirección web completa, por ejemplo https://www.inmobiliaria.com.ar")]
    [StringLength(256, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Sitio web")]
    public string? SitioWeb { get; set; }

    [StringLength(200, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Horario de atención")]
    public string? HorarioAtencion { get; set; }

    [StringLength(1000, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Descripción del negocio")]
    public string? Descripcion { get; set; }

    [StringLength(100, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Nombre del contacto de soporte")]
    public string? SoporteNombre { get; set; }

    [StringLength(50, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Teléfono o WhatsApp de soporte")]
    public string? SoporteTelefono { get; set; }

    [EmailAddress(ErrorMessage = "Ingresá un email válido.")]
    [StringLength(256, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Email de soporte")]
    public string? SoporteEmail { get; set; }

    /// <summary>Token de concurrencia (base64) para detectar si otro usuario guardó antes.</summary>
    public string? RowVersion { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(Cuit) && !Documentos.EsCuitValido(Cuit))
        {
            yield return new ValidationResult(
                "El CUIT no es válido: deben ser 11 dígitos con dígito verificador correcto.",
                [nameof(Cuit)]);
        }
    }
}
