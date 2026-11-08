using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Infrastructure.Configuracion;
using GestionAlquileres.Web.Models.Enums;
using GestionAlquileres.Web.ViewModels.Comun;

namespace GestionAlquileres.Web.Services.Contratos;

/// <summary>Datos mínimos de un contrato para calcular su estado visual.</summary>
public record DatosSemaforoContrato(
    EstadoContrato Estado,
    DateOnly FechaFin,
    bool TieneMoraActiva,
    bool TieneCuotaVencidaImpaga,
    DateOnly? ProximoVencimientoPendiente);

public record ResultadoSemaforo(EstadoContratoCalculado Estado, SemaforoViewModel Semaforo);

/// <summary>
/// Calcula el estado visible de un contrato a la fecha indicada. Función pura (sin base de datos ni reloj):
/// se testea directamente. Orden de prioridad:
/// 1. Rescindido / Finalizado (neutro).
/// 2. Vencido: vigente con fecha de fin pasada (crítico).
/// 3. En mora (mora aplicada sin cobrar) o pago atrasado (cuota vencida impaga) (crítico).
/// 4. Por vencer: termina dentro de <see cref="SemaforoOptions.DiasContratoPorVencer"/> días (atención).
/// 5. Pago próximo: una cuota vence dentro de <see cref="SemaforoOptions.DiasPagoProximo"/> días (atención).
/// 6. Al día.
/// </summary>
public static class CalculadoraSemaforo
{
    public static ResultadoSemaforo Calcular(DatosSemaforoContrato datos, DateOnly hoy, SemaforoOptions opciones)
    {
        switch (datos.Estado)
        {
            case EstadoContrato.Rescindido:
                return new(EstadoContratoCalculado.Rescindido, new(NivelSemaforo.Neutro, "Rescindido"));
            case EstadoContrato.Finalizado:
                return new(EstadoContratoCalculado.Finalizado, new(NivelSemaforo.Neutro, "Finalizado"));
        }

        if (datos.FechaFin < hoy)
        {
            return new(EstadoContratoCalculado.Vencido,
                new(NivelSemaforo.Critico, "Vencido", $"Terminó el {Formato.Fecha(datos.FechaFin)}"));
        }

        var porVencer = datos.FechaFin <= hoy.AddDays(opciones.DiasContratoPorVencer);
        var estado = porVencer ? EstadoContratoCalculado.PorVencer : EstadoContratoCalculado.Vigente;

        if (datos.TieneMoraActiva)
        {
            return new(estado, new(NivelSemaforo.Critico, "En mora", "Mora aplicada sin cobrar"));
        }

        if (datos.TieneCuotaVencidaImpaga)
        {
            return new(estado, new(NivelSemaforo.Critico, "Pago atrasado", "Cuota vencida sin pagar"));
        }

        if (porVencer)
        {
            return new(estado, new(NivelSemaforo.Atencion, "Por vencer", $"Termina el {Formato.Fecha(datos.FechaFin)}"));
        }

        if (datos.ProximoVencimientoPendiente is { } vencimiento && vencimiento <= hoy.AddDays(opciones.DiasPagoProximo))
        {
            return new(estado, new(NivelSemaforo.Atencion, "Pago próximo", $"Vence el {Formato.Fecha(vencimiento)}"));
        }

        return new(estado, new(NivelSemaforo.AlDia, "Al día"));
    }
}
