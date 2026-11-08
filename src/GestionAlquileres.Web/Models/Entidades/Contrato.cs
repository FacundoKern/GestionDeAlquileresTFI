using GestionAlquileres.Web.Models.Base;
using GestionAlquileres.Web.Models.Enums;

namespace GestionAlquileres.Web.Models.Entidades;

public class Contrato : EntidadNegocio
{
    public int PropiedadId { get; set; }

    public Propiedad Propiedad { get; set; } = null!;

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaFin { get; set; }

    public decimal MontoInicial { get; private set; }

    /// <summary>
    /// Monto vigente. Se guarda para consultar rápido, pero solo cambia a través de
    /// <see cref="RegistrarNuevoMonto"/>, que agrega el <see cref="HistorialMonto"/> en el mismo
    /// SaveChanges (misma transacción). Nunca se asigna por separado.
    /// </summary>
    public decimal MontoActual { get; private set; }

    public Moneda Moneda { get; set; } = Moneda.Ars;

    /// <summary>Día del mes en que vence el pago (1 a 28, para evitar problemas con febrero).</summary>
    public int DiaVencimiento { get; set; } = 10;

    public decimal? DepositoGarantia { get; set; }

    public EstadoContrato Estado { get; set; } = EstadoContrato.Vigente;

    public DateOnly? FechaRescision { get; set; }

    public string? MotivoRescision { get; set; }

    /// <summary>Contrato del que este es renovación (una renovación es un contrato nuevo).</summary>
    public int? ContratoAnteriorId { get; set; }

    public Contrato? ContratoAnterior { get; set; }

    public string? Observaciones { get; set; }

    public ICollection<ContratoInquilino> Inquilinos { get; set; } = new List<ContratoInquilino>();

    public ICollection<ContratoGarante> Garantes { get; set; } = new List<ContratoGarante>();

    public ConfiguracionAjuste? ConfiguracionAjuste { get; set; }

    public ICollection<HistorialMonto> HistorialMontos { get; set; } = new List<HistorialMonto>();

    public ICollection<Cuota> Cuotas { get; set; } = new List<Cuota>();

    public ICollection<Recibo> Recibos { get; set; } = new List<Recibo>();

    public ICollection<Mora> Moras { get; set; } = new List<Mora>();

    public ICollection<ContratoImpuesto> Impuestos { get; set; } = new List<ContratoImpuesto>();

    /// <summary>Fija el monto inicial y registra la primera entrada del historial.</summary>
    public HistorialMonto EstablecerMontoInicial(decimal monto, string usuario, DateTime ahoraUtc)
    {
        if (HistorialMontos.Count > 0)
        {
            throw new InvalidOperationException("El contrato ya tiene monto inicial.");
        }

        MontoInicial = monto;
        return RegistrarNuevoMonto(new HistorialMonto
        {
            MontoNuevo = monto,
            FechaVigencia = FechaInicio,
            AplicadoPor = usuario,
            AplicadoEnUtc = ahoraUtc,
            Observaciones = "Monto inicial del contrato",
        });
    }

    /// <summary>
    /// Único punto de cambio de <see cref="MontoActual"/>: agrega el historial y actualiza el monto
    /// en memoria; ambos se persisten juntos en el próximo SaveChanges.
    /// </summary>
    public HistorialMonto RegistrarNuevoMonto(HistorialMonto cambio)
    {
        if (cambio.MontoNuevo <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cambio), "El monto debe ser mayor a cero.");
        }

        cambio.MontoAnterior = MontoActual;
        cambio.Contrato = this;
        HistorialMontos.Add(cambio);
        MontoActual = cambio.MontoNuevo;
        return cambio;
    }
}
