using GestionAlquileres.Web.Models.Base;
using GestionAlquileres.Web.Models.Enums;

namespace GestionAlquileres.Web.Models.Entidades;

/// <summary>Cómo y cada cuánto se ajusta el monto de un contrato (1:1 con Contrato).</summary>
public class ConfiguracionAjuste : EntidadAuditable
{
    public int ContratoId { get; set; }

    public Contrato Contrato { get; set; } = null!;

    public TipoAjuste TipoAjuste { get; set; }

    /// <summary>Obligatorio si <see cref="TipoAjuste"/> es Indice.</summary>
    public int? IndiceId { get; set; }

    public Indice? Indice { get; set; }

    /// <summary>Obligatorio si <see cref="TipoAjuste"/> es PorcentajeFijo (p. ej. 10 = 10 %).</summary>
    public decimal? PorcentajeFijo { get; set; }

    public int PeriodicidadMeses { get; set; }

    public DateOnly ProximaFechaAjuste { get; set; }
}

/// <summary>Índice de actualización (ICL, IPC, Casa Propia...). Administrable: se pueden agregar otros.</summary>
public class Indice : EntidadNegocio
{
    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public PeriodicidadIndice Periodicidad { get; set; }

    public ICollection<IndiceValor> Valores { get; set; } = new List<IndiceValor>();
}

/// <summary>
/// Valor de un índice en una fecha, cargado manualmente. Para índices mensuales la fecha es el día 1
/// del mes. Se guarda el NIVEL del índice (no la variación), así el ajuste es siempre un cociente.
/// </summary>
public class IndiceValor : EntidadNegocio
{
    public int IndiceId { get; set; }

    public Indice Indice { get; set; } = null!;

    public DateOnly Fecha { get; set; }

    public decimal Valor { get; set; }
}

/// <summary>
/// Historial de montos del contrato. Solo se agregan filas (las correcciones son una fila nueva).
/// Se crea únicamente mediante <see cref="Contrato.RegistrarNuevoMonto"/>.
/// </summary>
public class HistorialMonto : ISoloAgregar
{
    public int Id { get; set; }

    public int ContratoId { get; set; }

    public Contrato Contrato { get; set; } = null!;

    public decimal MontoAnterior { get; set; }

    public decimal MontoNuevo { get; set; }

    public DateOnly FechaVigencia { get; set; }

    /// <summary>Null para el monto inicial.</summary>
    public TipoAjuste? TipoAjuste { get; set; }

    public int? IndiceId { get; set; }

    public Indice? Indice { get; set; }

    public decimal? ValorIndiceInicial { get; set; }

    public decimal? ValorIndiceFinal { get; set; }

    public decimal? Coeficiente { get; set; }

    public string? Observaciones { get; set; }

    /// <summary>Usuario que aplicó el cambio.</summary>
    public string AplicadoPor { get; set; } = string.Empty;

    public DateTime AplicadoEnUtc { get; set; }
}
