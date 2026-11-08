using GestionAlquileres.Web.Models.Base;
using GestionAlquileres.Web.Models.Enums;

namespace GestionAlquileres.Web.Models.Entidades;

/// <summary>
/// Inmueble administrado. El estado "alquilada/disponible" no se guarda: se deriva de si tiene un
/// contrato vigente. Solo se guarda <see cref="Activa"/> (una propiedad inactiva no se ofrece).
/// </summary>
public class Propiedad : EntidadNegocio
{
    public string Calle { get; set; } = string.Empty;

    public string Numero { get; set; } = string.Empty;

    public string? Piso { get; set; }

    public string? Departamento { get; set; }

    public string Localidad { get; set; } = string.Empty;

    public string Provincia { get; set; } = string.Empty;

    public string? CodigoPostal { get; set; }

    public TipoPropiedad Tipo { get; set; }

    public bool Activa { get; set; } = true;

    public string? Observaciones { get; set; }

    public ICollection<PropiedadPropietario> Propietarios { get; set; } = new List<PropiedadPropietario>();

    public ICollection<Contrato> Contratos { get; set; } = new List<Contrato>();

    public ICollection<FotoPropiedad> Fotos { get; set; } = new List<FotoPropiedad>();

    /// <summary>Dirección en una línea, p. ej. "Av. Corrientes 1234, 5° B, CABA". No se mapea.</summary>
    public string Direccion => FormatearDireccion(Calle, Numero, Piso, Departamento, Localidad);

    /// <summary>Estado derivado: inactiva si se marcó así; alquilada si tiene un contrato vigente; si no, disponible.</summary>
    public static EstadoPropiedad CalcularEstado(bool activa, bool tieneContratoVigente) =>
        !activa ? EstadoPropiedad.Inactiva
        : tieneContratoVigente ? EstadoPropiedad.Alquilada
        : EstadoPropiedad.Disponible;

    public static string FormatearDireccion(string calle, string numero, string? piso, string? departamento, string localidad)
    {
        var unidad = (piso, departamento) switch
        {
            ({ Length: > 0 }, { Length: > 0 }) => $", {piso}° {departamento}",
            ({ Length: > 0 }, _) => $", piso {piso}",
            (_, { Length: > 0 }) => $", depto. {departamento}",
            _ => string.Empty,
        };
        return $"{calle} {numero}{unidad}, {localidad}";
    }
}
