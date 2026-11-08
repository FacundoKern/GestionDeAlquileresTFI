using System.ComponentModel.DataAnnotations;

namespace GestionAlquileres.Web.Models.Enums;

public enum EstadoCuota
{
    [Display(Name = "Pendiente")] Pendiente = 1,
    [Display(Name = "Pagada parcialmente")] PagadaParcial = 2,
    [Display(Name = "Pagada")] Pagada = 3,
}

public enum EstadoRecibo
{
    [Display(Name = "Emitido")] Emitido = 1,
    [Display(Name = "Anulado")] Anulado = 2,
}

public enum ConceptoReciboItem
{
    [Display(Name = "Alquiler")] Alquiler = 1,
    [Display(Name = "Mora / punitorios")] Mora = 2,
    [Display(Name = "Impuesto")] Impuesto = 3,
    [Display(Name = "Expensas")] Expensas = 4,
    [Display(Name = "Otro")] Otro = 99,
}

public enum MedioPago
{
    [Display(Name = "Efectivo")] Efectivo = 1,
    [Display(Name = "Transferencia")] Transferencia = 2,
    [Display(Name = "Cheque")] Cheque = 3,
    [Display(Name = "Otro")] Otro = 99,
}

public enum CriterioMora
{
    [Display(Name = "Monto fijo")] MontoFijo = 1,
    [Display(Name = "Porcentaje sobre la cuota")] Porcentaje = 2,
    [Display(Name = "Interés diario")] InteresDiario = 3,
}

public enum EstadoMora
{
    [Display(Name = "Aplicada")] Aplicada = 1,
    [Display(Name = "Cobrada")] Cobrada = 2,
    [Display(Name = "Condonada")] Condonada = 3,
}

public enum TipoValorImpuesto
{
    [Display(Name = "Monto fijo")] MontoFijo = 1,
    [Display(Name = "Porcentaje")] Porcentaje = 2,
}
