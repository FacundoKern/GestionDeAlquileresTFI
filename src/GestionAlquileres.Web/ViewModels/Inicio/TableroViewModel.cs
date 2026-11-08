using GestionAlquileres.Web.ViewModels.Comun;

namespace GestionAlquileres.Web.ViewModels.Inicio;

public record TableroViewModel(
    int ContratosPorVencer,
    int AjustesProximos,
    int PagosPendientes,
    int MorasActivas,
    IReadOnlyList<ContratoAtencionViewModel> RequierenAtencion,
    int DiasContratoPorVencer,
    int DiasPagoProximo,
    int DiasProximoAjuste);

/// <param name="InquilinoId">Inquilino titular; null si el contrato no tiene titular.</param>
public record ContratoAtencionViewModel(
    int ContratoId,
    int? InquilinoId,
    string Inquilino,
    int PropiedadId,
    string Propiedad,
    SemaforoViewModel Semaforo);
