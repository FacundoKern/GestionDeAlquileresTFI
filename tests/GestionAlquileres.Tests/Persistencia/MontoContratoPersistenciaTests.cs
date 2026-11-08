using GestionAlquileres.Tests.Infraestructura;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Tests.Persistencia;

/// <summary>
/// Regla acordada: MontoActual se guarda siempre en la misma transacción que su HistorialMonto.
/// (El servicio de ajustes de la fase 2 agregará su propio test sobre esta misma regla.)
/// </summary>
[Collection(ColeccionBaseDeDatos.Nombre)]
public class MontoContratoPersistenciaTests
{
    private readonly BaseDeDatosFixture _bd;

    public MontoContratoPersistenciaTests(BaseDeDatosFixture bd)
    {
        _bd = bd;
    }

    [Fact]
    public async Task Monto_actual_e_historial_se_guardan_juntos()
    {
        var contrato = Crear.Contrato(montoInicial: 400_000m);
        await using (var db = _bd.CrearContexto())
        {
            db.Contratos.Add(contrato);
            await db.SaveChangesAsync();
        }

        await using (var db = _bd.CrearContexto())
        {
            var fila = await db.Contratos.Include(c => c.HistorialMontos).SingleAsync(c => c.Id == contrato.Id);
            fila.RegistrarNuevoMonto(new HistorialMonto
            {
                MontoNuevo = 440_000m,
                FechaVigencia = new DateOnly(2027, 1, 1),
                TipoAjuste = TipoAjuste.PorcentajeFijo,
                Coeficiente = 1.1m,
                AplicadoPor = "tests",
                AplicadoEnUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        await using (var db = _bd.CrearContexto())
        {
            var fila = await db.Contratos.Include(c => c.HistorialMontos).SingleAsync(c => c.Id == contrato.Id);
            Assert.Equal(440_000m, fila.MontoActual);
            Assert.Equal(400_000m, fila.MontoInicial);
            Assert.Equal([400_000m, 440_000m], fila.HistorialMontos.OrderBy(h => h.Id).Select(h => h.MontoNuevo));
            Assert.Equal(fila.MontoActual, fila.HistorialMontos.OrderBy(h => h.Id).Last().MontoNuevo);
        }
    }

    [Fact]
    public async Task Si_el_guardado_falla_no_cambia_ni_el_monto_ni_el_historial()
    {
        var contrato = Crear.Contrato(montoInicial: 400_000m);
        var dniRepetido = Crear.DniUnico();
        await using (var db = _bd.CrearContexto())
        {
            db.Contratos.Add(contrato);
            db.Propietarios.Add(Crear.Propietario(dniRepetido));
            await db.SaveChangesAsync();
        }

        await using (var db = _bd.CrearContexto())
        {
            var fila = await db.Contratos.Include(c => c.HistorialMontos).SingleAsync(c => c.Id == contrato.Id);
            fila.RegistrarNuevoMonto(new HistorialMonto { MontoNuevo = 999_000m, FechaVigencia = new DateOnly(2027, 1, 1), AplicadoPor = "tests" });
            db.Propietarios.Add(Crear.Propietario(dniRepetido)); // provoca un error en el mismo SaveChanges

            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        await using (var db = _bd.CrearContexto())
        {
            var fila = await db.Contratos.Include(c => c.HistorialMontos).SingleAsync(c => c.Id == contrato.Id);
            Assert.Equal(400_000m, fila.MontoActual);
            Assert.Single(fila.HistorialMontos);
        }
    }
}
