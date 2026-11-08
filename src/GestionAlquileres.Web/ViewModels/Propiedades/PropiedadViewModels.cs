using System.ComponentModel.DataAnnotations;
using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Models.Enums;
using GestionAlquileres.Web.ViewModels.Comun;
using GestionAlquileres.Web.ViewModels.Contratos;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GestionAlquileres.Web.ViewModels.Propiedades;

/// <summary>Filtros del listado (llegan por query string).</summary>
public class FiltroPropiedades
{
    /// <summary>Calle, número o localidad.</summary>
    public string? Texto { get; set; }

    /// <summary>Nombre, apellido, razón social o documento de alguno de sus propietarios.</summary>
    public string? Propietario { get; set; }

    public TipoPropiedad? Tipo { get; set; }

    public EstadoPropiedad? Estado { get; set; }

    public int Pagina { get; set; } = 1;
}

public record PropiedadFilaViewModel(int Id, string Direccion, TipoPropiedad Tipo, string Propietarios, EstadoPropiedad Estado, int? FotoPrincipalId);

public record PropiedadesIndexViewModel(FiltroPropiedades Filtro, ListaPaginada<PropiedadFilaViewModel> Resultados);

public record PropiedadDetalleViewModel(
    int Id,
    string Direccion,
    string Calle,
    string Numero,
    string? Piso,
    string? Departamento,
    string Localidad,
    string Provincia,
    string? CodigoPostal,
    TipoPropiedad Tipo,
    EstadoPropiedad Estado,
    string? Observaciones,
    IReadOnlyList<PropietarioDePropiedadViewModel> Propietarios,
    IReadOnlyList<ContratoResumenViewModel>? Contratos, // null = el rol no puede ver contratos
    string CreadoPor,
    DateTime CreadoEnUtc,
    string? ModificadoPor,
    DateTime? ModificadoEnUtc)
{
    /// <summary>Fotos de la propiedad (las completa el controlador con el servicio de fotos).</summary>
    public GaleriaFotosViewModel? Galeria { get; init; }
}

/// <summary>Galería de la ficha de la propiedad, con los límites para mostrarlos en el formulario de subida.</summary>
public record GaleriaFotosViewModel(
    int PropiedadId,
    string Direccion,
    IReadOnlyList<FotoPropiedadViewModel> Fotos,
    int MaximoPorPropiedad,
    int MaximoPorEnvio,
    int TamanioMaximoMB);

public record FotoPropiedadViewModel(int Id, string? Descripcion, bool EsPrincipal, int Ancho, int Alto, string RowVersion);

public record EliminarFotoViewModel(int Id, int PropiedadId, string Direccion, string? Descripcion, string RowVersion);

/// <summary>Resultado de subir varias fotos a la vez: las que entraron y un error por cada archivo rechazado.</summary>
public record ResultadoSubidaFotos(int Subidas, IReadOnlyList<string> Errores);

public record PropietarioDePropiedadViewModel(int Id, string Nombre, decimal Porcentaje);

public record EliminarPropiedadViewModel(int Id, string Direccion, string RowVersion, string? MotivoNoSePuede);

/// <summary>
/// Formulario de alta/edición. Solo expone campos editables (evita overposting); las opciones de los
/// desplegables no se bindean desde el formulario.
/// </summary>
public class PropiedadFormViewModel : IValidatableObject
{
    /// <summary>Cantidad de filas de propietarios del formulario (las vacías se ignoran).</summary>
    public const int FilasPropietarios = 4;

    public int Id { get; set; }

    [Required(ErrorMessage = "Ingresá la calle.")]
    [StringLength(150, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Calle")]
    public string Calle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá el número (o S/N).")]
    [StringLength(20, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Número")]
    public string Numero { get; set; } = string.Empty;

    [StringLength(10, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Piso")]
    public string? Piso { get; set; }

    [StringLength(10, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Departamento o unidad")]
    public string? Departamento { get; set; }

    [Required(ErrorMessage = "Ingresá la localidad o el barrio.")]
    [StringLength(100, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Localidad o barrio")]
    public string Localidad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Elegí la provincia.")]
    [StringLength(60, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Provincia")]
    public string Provincia { get; set; } = Provincias.Caba;

    [StringLength(10, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Código postal")]
    public string? CodigoPostal { get; set; }

    [Display(Name = "Tipo de propiedad")]
    public TipoPropiedad Tipo { get; set; } = TipoPropiedad.Departamento;

    [Display(Name = "Propiedad activa (se ofrece para alquilar)")]
    public bool Activa { get; set; } = true;

    [StringLength(2000, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }

    public List<PropietarioAsignadoViewModel> Propietarios { get; set; } = [];

    /// <summary>Token de concurrencia (base64).</summary>
    public string? RowVersion { get; set; }

    [BindNever]
    public IReadOnlyList<SelectListItem> OpcionesPropietarios { get; set; } = [];

    /// <summary>Filas con un propietario elegido (las vacías se ignoran).</summary>
    public IEnumerable<PropietarioAsignadoViewModel> PropietariosElegidos =>
        Propietarios.Where(p => p.PropietarioId is > 0);

    /// <summary>Completa con filas vacías hasta <see cref="FilasPropietarios"/> (al menos una vacía).</summary>
    public void CompletarFilasVacias()
    {
        Propietarios = PropietariosElegidos.ToList();
        var objetivo = Math.Max(FilasPropietarios, Propietarios.Count + 1);
        while (Propietarios.Count < objetivo)
        {
            Propietarios.Add(new PropietarioAsignadoViewModel());
        }
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var elegidos = Propietarios.Select((p, indice) => (Fila: p, Indice: indice)).Where(x => x.Fila.PropietarioId is > 0).ToList();

        if (elegidos.Count == 0)
        {
            yield return new ValidationResult("Elegí al menos un propietario.", [$"{nameof(Propietarios)}[0].{nameof(PropietarioAsignadoViewModel.PropietarioId)}"]);
            yield break;
        }

        foreach (var (fila, indice) in elegidos)
        {
            if (fila.Porcentaje is not (> 0 and <= 100))
            {
                yield return new ValidationResult(
                    $"Indicá la participación del propietario {indice + 1}: un número mayor a 0 y hasta 100.",
                    [$"{nameof(Propietarios)}[{indice}].{nameof(PropietarioAsignadoViewModel.Porcentaje)}"]);
            }
        }

        if (elegidos.GroupBy(x => x.Fila.PropietarioId).Any(g => g.Count() > 1))
        {
            yield return new ValidationResult("Elegiste el mismo propietario más de una vez.", [nameof(Propietarios)]);
        }

        var suma = elegidos.Sum(x => x.Fila.Porcentaje ?? 0);
        if (elegidos.All(x => x.Fila.Porcentaje is > 0) && suma != 100)
        {
            yield return new ValidationResult(
                $"Las participaciones tienen que sumar 100 % (ahora suman {Formato.Porcentaje(suma)}). " +
                "Para tres partes iguales podés usar 33,34 + 33,33 + 33,33.",
                [nameof(Propietarios)]);
        }
    }
}

public class PropietarioAsignadoViewModel
{
    [Display(Name = "Propietario")]
    public int? PropietarioId { get; set; }

    [Display(Name = "Participación (%)")]
    public decimal? Porcentaje { get; set; }
}
