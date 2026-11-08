using GestionAlquileres.Web.Models.Enums;

namespace GestionAlquileres.Web.Models.Base;

/// <summary>
/// Datos personales comunes a Propietario, Inquilino y Garante.
/// No tiene tabla propia: cada entidad concreta tiene la suya (son roles distintos con reglas distintas).
/// </summary>
public abstract class Persona : EntidadNegocio
{
    public TipoPersona TipoPersona { get; set; } = TipoPersona.Fisica;

    /// <summary>Nombre de pila (persona física) o razón social (persona jurídica).</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Solo para personas físicas.</summary>
    public string? Apellido { get; set; }

    public TipoDocumento TipoDocumento { get; set; } = TipoDocumento.Dni;

    /// <summary>Normalizado: sin puntos, guiones ni espacios.</summary>
    public string NumeroDocumento { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Telefono { get; set; }

    public string? Domicilio { get; set; }

    public string? Notas { get; set; }

    /// <summary>"Apellido, Nombre" o la razón social. No se mapea a la base (solo lectura).</summary>
    public string NombreCompleto => FormatearNombre(TipoPersona, Nombre, Apellido);

    /// <summary>Para usar en proyecciones de consultas (que no materializan la entidad).</summary>
    public static string FormatearNombre(TipoPersona tipo, string nombre, string? apellido) =>
        tipo == TipoPersona.Juridica || string.IsNullOrWhiteSpace(apellido) ? nombre : $"{apellido}, {nombre}";
}
