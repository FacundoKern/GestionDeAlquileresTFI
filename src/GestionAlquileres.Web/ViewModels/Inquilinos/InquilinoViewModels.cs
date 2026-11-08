using GestionAlquileres.Web.ViewModels.Comun;
using GestionAlquileres.Web.ViewModels.Contratos;
using GestionAlquileres.Web.ViewModels.Personas;

namespace GestionAlquileres.Web.ViewModels.Inquilinos;

public class InquilinoFormViewModel : PersonaFormViewModel;

/// <summary>Filtros del listado (llegan por query string).</summary>
public class FiltroInquilinos
{
    /// <summary>Nombre, apellido, razón social o número de documento.</summary>
    public string? Texto { get; set; }

    /// <summary>Calle, número o localidad de la propiedad de alguno de sus contratos (solo roles con acceso a contratos).</summary>
    public string? Propiedad { get; set; }

    /// <summary>true = con contrato vigente; false = sin contrato vigente; null = todos (solo roles con acceso a contratos).</summary>
    public bool? ConContratoVigente { get; set; }

    public int Pagina { get; set; } = 1;
}

/// <summary>
/// Fila del listado. <see cref="Contrato"/> es el contrato principal del inquilino (el vigente o, si no hay,
/// el más reciente); es null si no tiene contratos o si el rol no puede ver información de contratos.
/// </summary>
public record InquilinoFilaViewModel(
    int Id,
    string Nombre,
    string Documento,
    string? Telefono,
    string? Email,
    ContratoResumenViewModel? Contrato,
    int OtrosContratos);

/// <param name="MostrarContratos">false para los roles sin acceso a contratos (Consulta).</param>
public record InquilinosIndexViewModel(FiltroInquilinos Filtro, ListaPaginada<InquilinoFilaViewModel> Resultados, bool MostrarContratos);

public record InquilinoDetalleViewModel(
    int Id,
    PersonaDatosViewModel Datos,
    IReadOnlyList<ContratoResumenViewModel>? Contratos); // null = el rol no puede ver contratos
