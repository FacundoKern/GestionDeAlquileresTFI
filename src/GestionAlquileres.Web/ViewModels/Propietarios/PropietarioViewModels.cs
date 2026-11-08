using GestionAlquileres.Web.Models.Enums;
using GestionAlquileres.Web.ViewModels.Contratos;
using GestionAlquileres.Web.ViewModels.Personas;

namespace GestionAlquileres.Web.ViewModels.Propietarios;

public class PropietarioFormViewModel : PersonaFormViewModel;

/// <summary>Filtros del listado (llegan por query string).</summary>
public class FiltroPropietarios
{
    /// <summary>Nombre, apellido, razón social o número de documento.</summary>
    public string? Texto { get; set; }

    /// <summary>Calle, número o localidad de alguna de sus propiedades.</summary>
    public string? Propiedad { get; set; }

    public int Pagina { get; set; } = 1;
}

public record PropietarioFilaViewModel(int Id, string Nombre, string Documento, string? Telefono, string? Email, int CantidadPropiedades);

public record PropietariosIndexViewModel(FiltroPropietarios Filtro, ViewModels.Comun.ListaPaginada<PropietarioFilaViewModel> Resultados);

public record PropietarioDetalleViewModel(
    int Id,
    PersonaDatosViewModel Datos,
    IReadOnlyList<PropiedadDePropietarioViewModel> Propiedades,
    IReadOnlyList<ContratoResumenViewModel>? Contratos); // null = el rol no puede ver contratos

public record PropiedadDePropietarioViewModel(int Id, string Direccion, TipoPropiedad Tipo, decimal Porcentaje, EstadoPropiedad Estado);
