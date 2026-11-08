using System.ComponentModel.DataAnnotations;

namespace GestionAlquileres.Web.Models.Enums;

public enum Moneda
{
    [Display(Name = "Pesos (ARS)")] Ars = 1,
    [Display(Name = "Dólares (USD)")] Usd = 2,
}

/// <summary>
/// Estado guardado del contrato. "Por vencer" y "vencido" NO se guardan: se calculan a partir de las
/// fechas (ver <see cref="EstadoContratoCalculado"/>), para que nunca queden desactualizados.
/// </summary>
public enum EstadoContrato
{
    [Display(Name = "Vigente")] Vigente = 1,
    [Display(Name = "Finalizado")] Finalizado = 2,
    [Display(Name = "Rescindido")] Rescindido = 3,
}

/// <summary>Estado del contrato tal como se muestra al usuario (calculado a la fecha de hoy).</summary>
public enum EstadoContratoCalculado
{
    [Display(Name = "Vigente")] Vigente = 1,
    [Display(Name = "Por vencer")] PorVencer = 2,
    [Display(Name = "Vencido")] Vencido = 3,
    [Display(Name = "Finalizado")] Finalizado = 4,
    [Display(Name = "Rescindido")] Rescindido = 5,
}

public enum TipoGarantia
{
    [Display(Name = "Garantía propietaria")] Propietaria = 1,
    [Display(Name = "Recibo de sueldo")] ReciboSueldo = 2,
    [Display(Name = "Seguro de caución")] SeguroCaucion = 3,
    [Display(Name = "Otra")] Otra = 99,
}

public enum TipoAjuste
{
    [Display(Name = "Por índice")] Indice = 1,
    [Display(Name = "Porcentaje fijo")] PorcentajeFijo = 2,
    [Display(Name = "Manual")] Manual = 3,
}

public enum PeriodicidadIndice
{
    [Display(Name = "Diaria")] Diaria = 1,
    [Display(Name = "Mensual")] Mensual = 2,
}
