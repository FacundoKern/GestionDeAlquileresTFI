using System.Text.Json;
using GestionAlquileres.Tests.Infraestructura;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Tests.Persistencia;

[Collection(ColeccionBaseDeDatos.Nombre)]
public class AuditoriaTests
{
    private readonly BaseDeDatosFixture _bd;

    public AuditoriaTests(BaseDeDatosFixture bd)
    {
        _bd = bd;
    }

    private static Dictionary<string, JsonElement> Leer(string? json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json ?? "{}")!;

    [Fact]
    public async Task El_alta_registra_quien_y_los_valores_nuevos()
    {
        var usuario = new UsuarioDePrueba("alta@tests.test");
        var propietario = Crear.Propietario(nombre: "Creado");
        await using (var db = _bd.CrearContexto(usuario))
        {
            db.Propietarios.Add(propietario);
            await db.SaveChangesAsync();
        }

        await using (var db = _bd.CrearContexto())
        {
            var fila = await db.Propietarios.SingleAsync(p => p.Id == propietario.Id);
            Assert.Equal(usuario.Nombre, fila.CreadoPor);

            var registro = await db.AuditLogs.SingleAsync(a =>
                a.Entidad == "Propietario" && a.EntidadId == propietario.Id.ToString() && a.Accion == AccionAuditoria.Crear);
            Assert.Equal(usuario.Nombre, registro.UsuarioNombre);
            Assert.Equal(usuario.Id, registro.UsuarioId);
            Assert.Equal("Creado", Leer(registro.ValoresNuevos)["Nombre"].GetString());
            Assert.Null(registro.ValoresAnteriores);
        }
    }

    [Fact]
    public async Task La_modificacion_registra_solo_los_campos_que_cambiaron()
    {
        var propietario = Crear.Propietario();
        await using (var db = _bd.CrearContexto())
        {
            db.Propietarios.Add(propietario);
            await db.SaveChangesAsync();
        }

        var editor = new UsuarioDePrueba("editor@tests.test");
        await using (var db = _bd.CrearContexto(editor))
        {
            var fila = await db.Propietarios.SingleAsync(p => p.Id == propietario.Id);
            fila.Telefono = "11 5555-9999";
            fila.Nombre = fila.Nombre; // sin cambio real
            await db.SaveChangesAsync();
        }

        await using (var db = _bd.CrearContexto())
        {
            var fila = await db.Propietarios.SingleAsync(p => p.Id == propietario.Id);
            Assert.Equal(editor.Nombre, fila.ModificadoPor);
            Assert.Equal(UsuarioDePrueba.Operador.Nombre, fila.CreadoPor); // la creación no se pisa

            var registro = await db.AuditLogs.SingleAsync(a =>
                a.Entidad == "Propietario" && a.EntidadId == propietario.Id.ToString() && a.Accion == AccionAuditoria.Modificar);
            var antes = Leer(registro.ValoresAnteriores);
            var despues = Leer(registro.ValoresNuevos);
            Assert.Equal(["Telefono"], antes.Keys);
            Assert.Equal(JsonValueKind.Null, antes["Telefono"].ValueKind);
            Assert.Equal("11 5555-9999", despues["Telefono"].GetString());
        }
    }

    [Fact]
    public async Task Guardar_sin_cambios_reales_no_genera_registros()
    {
        var propietario = Crear.Propietario();
        await using (var db = _bd.CrearContexto())
        {
            db.Propietarios.Add(propietario);
            await db.SaveChangesAsync();
        }

        await using (var db = _bd.CrearContexto())
        {
            var fila = await db.Propietarios.SingleAsync(p => p.Id == propietario.Id);
            fila.Apellido = fila.Apellido;
            await db.SaveChangesAsync();

            Assert.Equal(1, await db.AuditLogs.CountAsync(a => a.Entidad == "Propietario" && a.EntidadId == propietario.Id.ToString()));
        }
    }

    [Fact]
    public async Task El_registro_de_auditoria_no_se_puede_modificar_ni_borrar()
    {
        await using (var db = _bd.CrearContexto())
        {
            db.Propietarios.Add(Crear.Propietario());
            await db.SaveChangesAsync();
        }

        await using (var db = _bd.CrearContexto())
        {
            var registro = await db.AuditLogs.OrderByDescending(a => a.Id).FirstAsync();
            registro.Detalle = "alterado";
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }

        await using (var db = _bd.CrearContexto())
        {
            db.AuditLogs.Remove(await db.AuditLogs.OrderByDescending(a => a.Id).FirstAsync());
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task El_historial_de_montos_no_se_puede_modificar()
    {
        var contrato = Crear.Contrato();
        await using (var db = _bd.CrearContexto())
        {
            db.Contratos.Add(contrato);
            await db.SaveChangesAsync();
        }

        await using (var db = _bd.CrearContexto())
        {
            var historial = await db.HistorialMontos.FirstAsync(h => h.ContratoId == contrato.Id);
            historial.MontoNuevo = 1m;
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task Un_recibo_emitido_no_se_edita_ni_se_elimina_pero_se_puede_anular()
    {
        var contrato = Crear.Contrato();
        var recibo = Crear.Recibo(contrato);
        await using (var db = _bd.CrearContexto())
        {
            db.Recibos.Add(recibo);
            await db.SaveChangesAsync();
        }

        await using (var db = _bd.CrearContexto())
        {
            (await db.Recibos.SingleAsync(r => r.Id == recibo.Id)).Total = 1m;
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }

        await using (var db = _bd.CrearContexto())
        {
            db.Recibos.Remove(await db.Recibos.SingleAsync(r => r.Id == recibo.Id));
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }

        await using (var db = _bd.CrearContexto())
        {
            var fila = await db.Recibos.SingleAsync(r => r.Id == recibo.Id);
            fila.Estado = EstadoRecibo.Anulado;
            fila.MotivoAnulacion = "Monto mal cargado";
            fila.AnuladoEnUtc = DateTime.UtcNow;
            fila.AnuladoPor = "tests";
            await db.SaveChangesAsync();

            Assert.True(await db.AuditLogs.AnyAsync(a =>
                a.Entidad == nameof(Recibo) && a.EntidadId == recibo.Id.ToString() && a.Accion == AccionAuditoria.Modificar));
        }
    }
}
