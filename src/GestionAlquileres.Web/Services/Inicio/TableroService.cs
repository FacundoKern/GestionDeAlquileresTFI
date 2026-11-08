using GestionAlquileres.Web.Data;
using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Infrastructure.Configuracion;
using GestionAlquileres.Web.Models.Base;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Models.Enums;
using GestionAlquileres.Web.Services.Contratos;
using GestionAlquileres.Web.ViewModels.Comun;
using GestionAlquileres.Web.ViewModels.Inicio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GestionAlquileres.Web.Services.Inicio;

public interface ITableroService
{
    Task<TableroViewModel> ObtenerAsync(CancellationToken ct = default);
}

/// <summary>Indicadores y alertas de la página de inicio.</summary>
public sealed class TableroService : ITableroService
{
    private const int MaximoAlertas = 10;

    private readonly AppDbContext _db;
    private readonly TimeProvider _reloj;
    private readonly SemaforoOptions _opciones;

    public TableroService(AppDbContext db, TimeProvider reloj, IOptions<SemaforoOptions> opciones)
    {
        _db = db;
        _reloj = reloj;
        _opciones = opciones.Value;
    }

    public async Task<TableroViewModel> ObtenerAsync(CancellationToken ct = default)
    {
        var hoy = _reloj.HoyEnArgentina();
        var limitePorVencer = hoy.AddDays(_opciones.DiasContratoPorVencer);
        var limitePago = hoy.AddDays(_opciones.DiasPagoProximo);
        var limiteAjuste = hoy.AddDays(_opciones.DiasProximoAjuste);

        // Los contratos vigentes son pocos (decenas o cientos): se traen proyectados y el semáforo se
        // calcula en memoria con la misma función que usan los listados.
        var vigentes = await _db.Contratos.AsNoTracking()
            .Where(c => c.Estado == EstadoContrato.Vigente)
            .Select(c => new
            {
                c.Id,
                c.Estado,
                c.FechaFin,
                ProximoAjuste = c.ConfiguracionAjuste != null ? c.ConfiguracionAjuste.ProximaFechaAjuste : (DateOnly?)null,
                Titular = c.Inquilinos
                    .Where(i => i.EsTitular)
                    .Select(i => new { i.InquilinoId, i.Inquilino.TipoPersona, i.Inquilino.Nombre, i.Inquilino.Apellido })
                    .FirstOrDefault(),
                c.PropiedadId,
                c.Propiedad.Calle,
                c.Propiedad.Numero,
                c.Propiedad.Piso,
                c.Propiedad.Departamento,
                c.Propiedad.Localidad,
                TieneMoraActiva = c.Moras.Any(m => m.Estado == EstadoMora.Aplicada),
                TieneCuotaVencidaImpaga = c.Cuotas.Any(q => q.Estado != EstadoCuota.Pagada && q.FechaVencimiento < hoy),
                ProximoVencimiento = c.Cuotas
                    .Where(q => q.Estado != EstadoCuota.Pagada && q.FechaVencimiento >= hoy)
                    .Min(q => (DateOnly?)q.FechaVencimiento),
            })
            .ToListAsync(ct);

        var conSemaforo = vigentes
            .Select(c => new
            {
                Contrato = c,
                Resultado = CalculadoraSemaforo.Calcular(
                    new DatosSemaforoContrato(c.Estado, c.FechaFin, c.TieneMoraActiva, c.TieneCuotaVencidaImpaga, c.ProximoVencimiento),
                    hoy,
                    _opciones),
            })
            .ToList();

        var requierenAtencion = conSemaforo
            .Where(x => x.Resultado.Semaforo.Nivel >= NivelSemaforo.Atencion)
            .OrderByDescending(x => x.Resultado.Semaforo.Nivel)
            .ThenBy(x => x.Contrato.FechaFin)
            .Take(MaximoAlertas)
            .Select(x => new ContratoAtencionViewModel(
                x.Contrato.Id,
                x.Contrato.Titular?.InquilinoId,
                x.Contrato.Titular is { } t ? Persona.FormatearNombre(t.TipoPersona, t.Nombre, t.Apellido) : "Sin inquilino titular",
                x.Contrato.PropiedadId,
                Propiedad.FormatearDireccion(x.Contrato.Calle, x.Contrato.Numero, x.Contrato.Piso, x.Contrato.Departamento, x.Contrato.Localidad),
                x.Resultado.Semaforo))
            .ToList();

        var pagosPendientes = await _db.Cuotas.AsNoTracking()
            .CountAsync(q => q.Contrato.Estado == EstadoContrato.Vigente
                && q.Estado != EstadoCuota.Pagada
                && q.FechaVencimiento <= limitePago, ct);

        var morasActivas = await _db.Moras.AsNoTracking()
            .CountAsync(m => m.Estado == EstadoMora.Aplicada, ct);

        return new TableroViewModel(
            ContratosPorVencer: vigentes.Count(c => c.FechaFin >= hoy && c.FechaFin <= limitePorVencer),
            AjustesProximos: vigentes.Count(c => c.ProximoAjuste is { } fecha && fecha <= limiteAjuste && fecha <= c.FechaFin),
            PagosPendientes: pagosPendientes,
            MorasActivas: morasActivas,
            RequierenAtencion: requierenAtencion,
            DiasContratoPorVencer: _opciones.DiasContratoPorVencer,
            DiasPagoProximo: _opciones.DiasPagoProximo,
            DiasProximoAjuste: _opciones.DiasProximoAjuste);
    }
}
