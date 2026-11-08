using GestionAlquileres.Web.Models.Base;

namespace GestionAlquileres.Web.Models.Entidades;

public class Inquilino : Persona
{
    public ICollection<ContratoInquilino> Contratos { get; set; } = new List<ContratoInquilino>();
}
