namespace GestionAlquileres.Web.Models.Entidades;

/// <summary>Copropiedad: una propiedad puede tener varios propietarios con su porcentaje (suma 100).</summary>
public class PropiedadPropietario
{
    public int PropiedadId { get; set; }

    public Propiedad Propiedad { get; set; } = null!;

    public int PropietarioId { get; set; }

    public Propietario Propietario { get; set; } = null!;

    public decimal Porcentaje { get; set; }
}
