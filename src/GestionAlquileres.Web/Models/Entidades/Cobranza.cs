using GestionAlquileres.Web.Models.Base;
using GestionAlquileres.Web.Models.Enums;

namespace GestionAlquileres.Web.Models.Entidades;

/// <summary>
/// Obligación de pago de un período (mes) de un contrato. Permite saber qué se debe, no solo qué se
/// pagó: es la base de "pagos pendientes" y de la bandeja de mora. Admite pagos parciales (varios recibos).
/// </summary>
public class Cuota : EntidadNegocio
{
    public int ContratoId { get; set; }

    public Contrato Contrato { get; set; } = null!;

    /// <summary>Primer día del mes al que corresponde la cuota.</summary>
    public DateOnly Periodo { get; set; }

    public DateOnly FechaVencimiento { get; set; }

    public decimal Monto { get; set; }

    public EstadoCuota Estado { get; set; } = EstadoCuota.Pendiente;

    public ICollection<Recibo> Recibos { get; set; } = new List<Recibo>();

    public ICollection<Mora> Moras { get; set; } = new List<Mora>();
}

/// <summary>
/// Recibo de pago (registra el pago). No es un comprobante fiscal. Una vez emitido no se edita ni se
/// elimina: solo puede anularse (con motivo) y, si corresponde, se emite uno nuevo que lo reemplaza.
/// El número es correlativo y sin huecos (ver <see cref="Numerador"/>); los anulados conservan su número.
/// </summary>
public class Recibo : EntidadAuditable, INoEliminable, IInmutableTrasEmision
{
    public long Numero { get; set; }

    public int ContratoId { get; set; }

    public Contrato Contrato { get; set; } = null!;

    public int? CuotaId { get; set; }

    public Cuota? Cuota { get; set; }

    public DateOnly FechaEmision { get; set; }

    public DateOnly FechaPago { get; set; }

    public Moneda Moneda { get; set; }

    public decimal Total { get; set; }

    public MedioPago MedioPago { get; set; }

    public string? Observaciones { get; set; }

    [ModificableTrasEmision]
    public EstadoRecibo Estado { get; set; } = EstadoRecibo.Emitido;

    [ModificableTrasEmision]
    public string? MotivoAnulacion { get; set; }

    [ModificableTrasEmision]
    public DateTime? AnuladoEnUtc { get; set; }

    [ModificableTrasEmision]
    public string? AnuladoPor { get; set; }

    [ModificableTrasEmision]
    public int? ReemplazadoPorReciboId { get; set; }

    public Recibo? ReemplazadoPorRecibo { get; set; }

    public ICollection<ReciboItem> Items { get; set; } = new List<ReciboItem>();
}

/// <summary>Línea de un recibo (alquiler, mora, impuesto...). Inmutable.</summary>
public class ReciboItem : ISoloAgregar
{
    public int Id { get; set; }

    public int ReciboId { get; set; }

    public Recibo Recibo { get; set; } = null!;

    public ConceptoReciboItem Concepto { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    public int? ImpuestoId { get; set; }

    public Impuesto? Impuesto { get; set; }

    public int? MoraId { get; set; }

    public Mora? Mora { get; set; }
}

/// <summary>
/// Contador para numeración correlativa sin huecos. Se incrementa con bloqueo de fila dentro de la
/// misma transacción que el recibo (una SEQUENCE de SQL Server deja huecos si la transacción se revierte).
/// </summary>
[NoAuditar]
public class Numerador
{
    public const string Recibos = "RECIBO";

    public string Clave { get; set; } = string.Empty;

    public long UltimoValor { get; set; }
}
