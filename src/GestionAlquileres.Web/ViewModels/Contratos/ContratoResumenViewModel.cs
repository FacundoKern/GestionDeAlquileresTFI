using GestionAlquileres.Web.ViewModels.Comun;

namespace GestionAlquileres.Web.ViewModels.Contratos;

/// <summary>Contrato resumido para las fichas (propietario, propiedad, inquilino, garante).</summary>
public record ContratoResumenViewModel(
    int Id,
    int PropiedadId,
    string Propiedad,
    string Inquilinos,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    string MontoActual,
    SemaforoViewModel Estado);
