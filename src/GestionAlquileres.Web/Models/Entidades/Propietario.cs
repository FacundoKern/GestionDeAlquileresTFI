using GestionAlquileres.Web.Models.Base;

namespace GestionAlquileres.Web.Models.Entidades;

public class Propietario : Persona
{
    public ICollection<PropiedadPropietario> Propiedades { get; set; } = new List<PropiedadPropietario>();
}
