using GestionAlquileres.Web.Models.Enums;

namespace GestionAlquileres.Web.Models.Entidades;

/// <summary>Coinquilinos: un contrato puede tener varios inquilinos; uno es el titular.</summary>
public class ContratoInquilino
{
    public int ContratoId { get; set; }

    public Contrato Contrato { get; set; } = null!;

    public int InquilinoId { get; set; }

    public Inquilino Inquilino { get; set; } = null!;

    public bool EsTitular { get; set; }
}

/// <summary>Garante de un contrato, con el tipo de garantía que presenta para ese contrato.</summary>
public class ContratoGarante
{
    public int ContratoId { get; set; }

    public Contrato Contrato { get; set; } = null!;

    public int GaranteId { get; set; }

    public Garante Garante { get; set; } = null!;

    public TipoGarantia TipoGarantia { get; set; }

    /// <summary>Matrícula del inmueble, número de póliza, empleador, etc.</summary>
    public string? Detalle { get; set; }
}

/// <summary>Impuestos/conceptos que se agregan como ítems en los recibos de este contrato.</summary>
public class ContratoImpuesto
{
    public int ContratoId { get; set; }

    public Contrato Contrato { get; set; } = null!;

    public int ImpuestoId { get; set; }

    public Impuesto Impuesto { get; set; } = null!;
}
