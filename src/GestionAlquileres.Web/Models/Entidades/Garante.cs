using GestionAlquileres.Web.Models.Base;

namespace GestionAlquileres.Web.Models.Entidades;

/// <summary>
/// Garante. El tipo de garantía no está acá sino en <see cref="ContratoGarante"/>:
/// la misma persona puede garantizar distintos contratos con distintas garantías.
/// </summary>
public class Garante : Persona
{
    public ICollection<ContratoGarante> Contratos { get; set; } = new List<ContratoGarante>();
}
