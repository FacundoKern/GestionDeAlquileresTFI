using System.Linq.Expressions;
using GestionAlquileres.Web.Data;
using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Infrastructure.Configuracion;
using GestionAlquileres.Web.Models.Base;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Models.Enums;
using GestionAlquileres.Web.ViewModels.Contratos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GestionAlquileres.Web.Services.Contratos;

public interface IResumenContratosService
{
    /// <summary>Contratos que cumplen el filtro, con su semáforo calculado a hoy. Vigentes primero.</summary>
    Task<IReadOnlyList<ContratoResumenViewModel>> ObtenerAsync(Expression<Func<Contrato, bool>> filtro, CancellationToken ct = default);
}

public sealed class ResumenContratosService : IResumenContratosService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _reloj;
    private readonly SemaforoOptions _opciones;

    public ResumenContratosService(AppDbContext db, TimeProvider reloj, IOptions<SemaforoOptions> opciones)
    {
        _db = db;
        _reloj = reloj;
        _opciones = opciones.Value;
    }

    public async Task<IReadOnlyList<ContratoResumenViewModel>> ObtenerAsync(
        Expression<Func<Contrato, bool>> filtro,
        CancellationToken ct = default)
    {
        var hoy = _reloj.HoyEnArgentina();

        var contratos = await _db.Contratos.AsNoTracking()
            .Where(filtro)
            .Select(c => new
            {
                c.Id,
                c.PropiedadId,
                c.Propiedad.Calle,
                c.Propiedad.Numero,
                c.Propiedad.Piso,
                c.Propiedad.Departamento,
                c.Propiedad.Localidad,
                Inquilinos = c.Inquilinos
                    .OrderByDescending(i => i.EsTitular)
                    .Select(i => new { i.Inquilino.TipoPersona, i.Inquilino.Nombre, i.Inquilino.Apellido })
                    .ToList(),
                c.FechaInicio,
                c.FechaFin,
                c.MontoActual,
                c.Moneda,
                c.Estado,
                TieneMoraActiva = c.Moras.Any(m => m.Estado == EstadoMora.Aplicada),
                TieneCuotaVencidaImpaga = c.Cuotas.Any(q => q.Estado != EstadoCuota.Pagada && q.FechaVencimiento < hoy),
                ProximoVencimiento = c.Cuotas
                    .Where(q => q.Estado != EstadoCuota.Pagada && q.FechaVencimiento >= hoy)
                    .Min(q => (DateOnly?)q.FechaVencimiento),
            })
            .ToListAsync(ct);

        return contratos
            .OrderBy(c => c.Estado == EstadoContrato.Vigente ? 0 : 1)
            .ThenByDescending(c => c.FechaInicio)
            .Select(c =>
            {
                var titular = c.Inquilinos.FirstOrDefault();
                var inquilinos = titular is null
                    ? "Sin inquilinos"
                    : Persona.FormatearNombre(titular.TipoPersona, titular.Nombre, titular.Apellido)
                      + (c.Inquilinos.Count > 1 ? $" y {c.Inquilinos.Count - 1} más" : string.Empty);

                var semaforo = CalculadoraSemaforo.Calcular(
                    new DatosSemaforoContrato(c.Estado, c.FechaFin, c.TieneMoraActiva, c.TieneCuotaVencidaImpaga, c.ProximoVencimiento),
                    hoy,
                    _opciones).Semaforo;

                return new ContratoResumenViewModel(
                    c.Id,
                    c.PropiedadId,
                    Propiedad.FormatearDireccion(c.Calle, c.Numero, c.Piso, c.Departamento, c.Localidad),
                    inquilinos,
                    c.FechaInicio,
                    c.FechaFin,
                    Formato.Moneda(c.MontoActual, c.Moneda),
                    semaforo);
            })
            .ToList();
    }
}
