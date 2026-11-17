using System.ComponentModel.DataAnnotations;
using GestionAlquileres.Web.Models.Enums;
using GestionAlquileres.Web.ViewModels.Comun;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GestionAlquileres.Web.ViewModels.Contratos;

public class FiltroContratos
{
    /// <summary>Dirección de la propiedad o nombre de alguno de los inquilinos.</summary>
    public string? Texto { get; set; }

    public EstadoContrato? Estado { get; set; }

    public int Pagina { get; set; } = 1;
}

public record ContratosIndexViewModel(FiltroContratos Filtro, ListaPaginada<ContratoResumenViewModel> Resultados);

/// <summary>Alta de contrato. Solo los campos editables: el monto actual y el historial los arma el servicio.</summary>
public class ContratoFormViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Elegí la propiedad.")]
    [Display(Name = "Propiedad")]
    public int? PropiedadId { get; set; }

    [Required(ErrorMessage = "Elegí el inquilino titular.")]
    [Display(Name = "Inquilino titular")]
    public int? InquilinoTitularId { get; set; }

    [Display(Name = "Coinquilinos")]
    // Nullable: si no, MVC lo marca como obligatorio en el cliente (los coinquilinos son opcionales).
    public List<int>? CoinquilinosIds { get; set; }

    [Display(Name = "Garante")]
    public int? GaranteId { get; set; }

    [Display(Name = "Tipo de garantía")]
    public TipoGarantia? TipoGarantia { get; set; }

    [StringLength(200, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Detalle de la garantía")]
    public string? DetalleGarantia { get; set; }

    [Required(ErrorMessage = "Ingresá la fecha de inicio.")]
    [Display(Name = "Fecha de inicio")]
    public DateOnly? FechaInicio { get; set; }

    [Required(ErrorMessage = "Ingresá la fecha de fin.")]
    [Display(Name = "Fecha de fin")]
    public DateOnly? FechaFin { get; set; }

    [Display(Name = "Moneda")]
    public Moneda Moneda { get; set; } = Moneda.Ars;

    [Required(ErrorMessage = "Ingresá el monto mensual.")]
    [Range(typeof(decimal), "0.01", "999999999", ParseLimitsInInvariantCulture = true, ErrorMessage = "El monto tiene que ser mayor a cero.")]
    [Display(Name = "Monto mensual inicial")]
    public decimal? MontoInicial { get; set; }

    [Range(1, 28, ErrorMessage = "Elegí un día entre {1} y {2}.")]
    [Display(Name = "Día de vencimiento del pago")]
    public int DiaVencimiento { get; set; } = 10;

    [Range(typeof(decimal), "0", "999999999", ParseLimitsInInvariantCulture = true, ErrorMessage = "El depósito no puede ser negativo.")]
    [Display(Name = "Depósito en garantía")]
    public decimal? DepositoGarantia { get; set; }

    [Display(Name = "Tipo de ajuste")]
    public TipoAjuste TipoAjuste { get; set; } = TipoAjuste.Indice;

    [Display(Name = "Índice")]
    public int? IndiceId { get; set; }

    [Range(typeof(decimal), "0.01", "1000", ParseLimitsInInvariantCulture = true, ErrorMessage = "El porcentaje tiene que ser mayor a cero y como máximo 1000.")]
    [Display(Name = "Porcentaje de ajuste")]
    public decimal? PorcentajeFijo { get; set; }

    [Range(1, 24, ErrorMessage = "Elegí entre {1} y {2} meses.")]
    [Display(Name = "Se ajusta cada (meses)")]
    public int PeriodicidadMeses { get; set; } = 3;

    [StringLength(2000, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }

    [BindNever]
    public IReadOnlyList<SelectListItem> OpcionesPropiedades { get; set; } = [];

    [BindNever]
    public IReadOnlyList<SelectListItem> OpcionesInquilinos { get; set; } = [];

    [BindNever]
    public IReadOnlyList<SelectListItem> OpcionesGarantes { get; set; } = [];

    [BindNever]
    public IReadOnlyList<SelectListItem> OpcionesIndices { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaInicio is { } inicio && FechaFin is { } fin && fin <= inicio)
        {
            yield return new ValidationResult("La fecha de fin tiene que ser posterior a la de inicio.", [nameof(FechaFin)]);
        }

        if (InquilinoTitularId is { } titular && CoinquilinosIds?.Contains(titular) == true)
        {
            yield return new ValidationResult("El titular no puede ser también coinquilino.", [nameof(CoinquilinosIds)]);
        }

        if (GaranteId is not null && TipoGarantia is null)
        {
            yield return new ValidationResult("Elegí el tipo de garantía.", [nameof(TipoGarantia)]);
        }

        if (TipoAjuste == TipoAjuste.Indice && IndiceId is null)
        {
            yield return new ValidationResult("Elegí el índice con el que se ajusta.", [nameof(IndiceId)]);
        }

        if (TipoAjuste == TipoAjuste.PorcentajeFijo && PorcentajeFijo is null)
        {
            yield return new ValidationResult("Ingresá el porcentaje de ajuste.", [nameof(PorcentajeFijo)]);
        }
    }
}

public record PersonaDelContratoViewModel(int Id, string Nombre, string Documento, string? Rol);

public record HistorialMontoViewModel(DateOnly FechaVigencia, string MontoAnterior, string MontoNuevo, string Motivo);

public record DocumentoContratoViewModel(int Id, string Descripcion, string? NombreOriginal, string Tamanio, string SubidoPor, DateTime SubidoEnUtc);

public record ContratoDetalleViewModel(
    int Id,
    int PropiedadId,
    string Propiedad,
    SemaforoViewModel Estado,
    EstadoContrato EstadoGuardado,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    string MontoInicial,
    string MontoActual,
    Moneda Moneda,
    int DiaVencimiento,
    string? DepositoGarantia,
    string Ajuste,
    DateOnly? ProximoAjuste,
    DateOnly? FechaRescision,
    string? MotivoRescision,
    string? Observaciones,
    IReadOnlyList<PersonaDelContratoViewModel> Inquilinos,
    IReadOnlyList<PersonaDelContratoViewModel> Garantes,
    IReadOnlyList<HistorialMontoViewModel> Historial,
    IReadOnlyList<DocumentoContratoViewModel> Documentos,
    string? Advertencia,
    int TamanioMaximoDocumentoMB)
{
    public bool PuedeRescindirse => EstadoGuardado == EstadoContrato.Vigente;
}

public class RescindirContratoViewModel
{
    [BindNever]
    public int Id { get; set; }

    [BindNever]
    public string Propiedad { get; set; } = string.Empty;

    [BindNever]
    public DateOnly FechaInicio { get; set; }

    [BindNever]
    public DateOnly FechaFin { get; set; }

    [Required(ErrorMessage = "Ingresá la fecha de rescisión.")]
    [Display(Name = "Fecha de rescisión")]
    public DateOnly? FechaRescision { get; set; }

    [Required(ErrorMessage = "Contá el motivo de la rescisión.")]
    [StringLength(1000, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Motivo")]
    public string? Motivo { get; set; }

    public string? RowVersion { get; set; }
}

public record EliminarDocumentoViewModel(int Id, int ContratoId, string Descripcion, string Propiedad, string RowVersion);
