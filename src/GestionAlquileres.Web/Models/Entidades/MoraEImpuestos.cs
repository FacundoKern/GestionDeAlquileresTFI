using GestionAlquileres.Web.Models.Base;
using GestionAlquileres.Web.Models.Enums;

namespace GestionAlquileres.Web.Models.Entidades;

/// <summary>
/// Criterio de cálculo de mora con vigencia. Con <see cref="ContratoId"/> null es la configuración
/// general; con valor, sobrescribe la general para ese contrato. No se edita: para cambiarla se cierra la
/// vigencia (<see cref="VigenciaHasta"/>) y se crea una nueva, así los cálculos pasados son reproducibles.
/// </summary>
public class ConfiguracionMora : EntidadNegocio
{
    public int? ContratoId { get; set; }

    public Contrato? Contrato { get; set; }

    public CriterioMora Criterio { get; set; }

    /// <summary>Monto, porcentaje (10 = 10 %) o tasa diaria (0,1 = 0,1 % diario) según el criterio.</summary>
    public decimal Valor { get; set; }

    public int DiasGracia { get; set; }

    public DateOnly VigenciaDesde { get; set; }

    public DateOnly? VigenciaHasta { get; set; }
}

/// <summary>
/// Mora aplicada MANUALMENTE por el administrador sobre una cuota. El sistema sugiere
/// <see cref="MontoSugerido"/>; el administrador decide <see cref="MontoAplicado"/>.
/// El usuario que la aplicó queda en <see cref="EntidadAuditable.CreadoPor"/>.
/// </summary>
public class Mora : EntidadNegocio
{
    public int ContratoId { get; set; }

    public Contrato Contrato { get; set; } = null!;

    public int CuotaId { get; set; }

    public Cuota Cuota { get; set; } = null!;

    public int DiasAtraso { get; set; }

    public decimal MontoSugerido { get; set; }

    public decimal MontoAplicado { get; set; }

    public DateOnly FechaAplicacion { get; set; }

    /// <summary>Configuración usada para la sugerencia (trazabilidad).</summary>
    public int ConfiguracionMoraId { get; set; }

    public ConfiguracionMora ConfiguracionMora { get; set; } = null!;

    public EstadoMora Estado { get; set; } = EstadoMora.Aplicada;

    public string? Motivo { get; set; }
}

/// <summary>
/// Impuesto o concepto adicional que se agrega como ítem en los recibos de los contratos que lo tengan
/// asignado. Los valores tienen vigencia (<see cref="ImpuestoVigencia"/>).
/// PENDIENTE: confirmar con el cliente qué impuestos son y cómo se aplican (antes de la fase 4).
/// </summary>
public class Impuesto : EntidadNegocio
{
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<ImpuestoVigencia> Vigencias { get; set; } = new List<ImpuestoVigencia>();

    public ICollection<ContratoImpuesto> Contratos { get; set; } = new List<ContratoImpuesto>();
}

/// <summary>Valor de un impuesto vigente entre dos fechas. Para modificarlo se cierra y se crea otra vigencia.</summary>
public class ImpuestoVigencia : EntidadNegocio
{
    public int ImpuestoId { get; set; }

    public Impuesto Impuesto { get; set; } = null!;

    public TipoValorImpuesto TipoValor { get; set; }

    public decimal Valor { get; set; }

    public DateOnly VigenciaDesde { get; set; }

    public DateOnly? VigenciaHasta { get; set; }
}
