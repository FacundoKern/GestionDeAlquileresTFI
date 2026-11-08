using GestionAlquileres.Web.Models.Enums;
using GestionAlquileres.Web.ViewModels.Comun;
using GestionAlquileres.Web.ViewModels.Contratos;
using GestionAlquileres.Web.ViewModels.Personas;

namespace GestionAlquileres.Web.ViewModels.Garantes;

public class GaranteFormViewModel : PersonaFormViewModel;

/// <summary>Filtros del listado (llegan por query string).</summary>
public class FiltroGarantes
{
    /// <summary>Nombre, apellido, razón social o número de documento.</summary>
    public string? Texto { get; set; }

    /// <summary>true = garantiza algún contrato vigente; false = ninguno; null = todos (solo roles con acceso a contratos).</summary>
    public bool? GarantizaContratoVigente { get; set; }

    public int Pagina { get; set; } = 1;
}

/// <summary>Fila del listado. Las cantidades de contratos son null si el rol no puede ver contratos.</summary>
public record GaranteFilaViewModel(
    int Id,
    string Nombre,
    string Documento,
    string? Telefono,
    string? Email,
    int? ContratosVigentes,
    int? ContratosTotales);

/// <param name="MostrarContratos">false para los roles sin acceso a contratos (Consulta).</param>
public record GarantesIndexViewModel(FiltroGarantes Filtro, ListaPaginada<GaranteFilaViewModel> Resultados, bool MostrarContratos);

public record GaranteDetalleViewModel(
    int Id,
    PersonaDatosViewModel Datos,
    IReadOnlyList<ContratoGarantizadoViewModel>? Contratos); // null = el rol no puede ver contratos

/// <summary>Contrato que garantiza, con la garantía que presentó para ese contrato.</summary>
public record ContratoGarantizadoViewModel(ContratoResumenViewModel Contrato, TipoGarantia TipoGarantia, string? DetalleGarantia);
