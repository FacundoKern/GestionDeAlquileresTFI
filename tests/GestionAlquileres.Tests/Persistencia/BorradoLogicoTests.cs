using GestionAlquileres.Tests.Infraestructura;
using GestionAlquileres.Web.Data;
using GestionAlquileres.Web.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Tests.Persistencia;

[Collection(ColeccionBaseDeDatos.Nombre)]
public class BorradoLogicoTests
{
    private readonly BaseDeDatosFixture _bd;

    public BorradoLogicoTests(BaseDeDatosFixture bd)
    {
        _bd = bd;
    }

    [Fact]
    public async Task Eliminar_marca_el_registro_en_lugar_de_borrarlo()
    {
        var propietario = Crear.Propietario();
        await using (var db = _bd.CrearContexto())
        {
            db.Propietarios.Add(propietario);
            await db.SaveChangesAsync();
        }

        await using (var db = _bd.CrearContexto())
        {
            db.Propietarios.Remove(await db.Propietarios.SingleAsync(p => p.Id == propietario.Id));
            await db.SaveChangesAsync();
        }

        await using (var db = _bd.CrearContexto())
        {
            Assert.False(await db.Propietarios.AnyAsync(p => p.Id == propietario.Id)); // oculto por el filtro global

            var fila = await db.Propietarios.IgnoreQueryFilters().SingleAsync(p => p.Id == propietario.Id);
            Assert.True(fila.Eliminado);
            Assert.NotNull(fila.EliminadoEnUtc);
            Assert.Equal(UsuarioDePrueba.Operador.Nombre, fila.EliminadoPor);
        }
    }

    [Fact]
    public async Task La_eliminacion_queda_auditada_con_los_valores_anteriores()
    {
        var propietario = Crear.Propietario(nombre: "Eliminable");
        await using (var db = _bd.CrearContexto())
        {
            db.Propietarios.Add(propietario);
            await db.SaveChangesAsync();
            db.Propietarios.Remove(propietario);
            await db.SaveChangesAsync();
        }

        await using (var db = _bd.CrearContexto())
        {
            var registro = await db.AuditLogs.SingleAsync(a =>
                a.Entidad == "Propietario" && a.EntidadId == propietario.Id.ToString() && a.Accion == AccionAuditoria.Eliminar);
            Assert.Contains("Eliminable", registro.ValoresAnteriores);
            Assert.Equal(UsuarioDePrueba.Operador.Nombre, registro.UsuarioNombre);
        }
    }

    [Fact]
    public async Task Se_puede_volver_a_cargar_un_documento_eliminado()
    {
        var dni = Crear.DniUnico();
        await using (var db = _bd.CrearContexto())
        {
            var original = Crear.Propietario(dni);
            db.Propietarios.Add(original);
            await db.SaveChangesAsync();
            db.Propietarios.Remove(original);
            await db.SaveChangesAsync();
        }

        await using (var db = _bd.CrearContexto())
        {
            db.Propietarios.Add(Crear.Propietario(dni));
            await db.SaveChangesAsync(); // el índice único solo cuenta los no eliminados
        }
    }

    [Fact]
    public async Task No_puede_haber_dos_activos_con_el_mismo_documento()
    {
        var dni = Crear.DniUnico();
        await using var db = _bd.CrearContexto();
        db.Propietarios.Add(Crear.Propietario(dni));
        await db.SaveChangesAsync();

        db.Propietarios.Add(Crear.Propietario(dni));
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.True(ErroresBaseDeDatos.EsViolacionDeUnicidad(error));
    }

    [Fact]
    public async Task Restaurar_un_eliminado_se_audita_como_restauracion()
    {
        var propietario = Crear.Propietario();
        await using (var db = _bd.CrearContexto())
        {
            db.Propietarios.Add(propietario);
            await db.SaveChangesAsync();
            db.Propietarios.Remove(propietario);
            await db.SaveChangesAsync();
        }

        await using (var db = _bd.CrearContexto())
        {
            var fila = await db.Propietarios.IgnoreQueryFilters().SingleAsync(p => p.Id == propietario.Id);
            fila.Eliminado = false;
            await db.SaveChangesAsync();

            Assert.True(await db.Propietarios.AnyAsync(p => p.Id == propietario.Id));
            Assert.True(await db.AuditLogs.AnyAsync(a =>
                a.Entidad == "Propietario" && a.EntidadId == propietario.Id.ToString() && a.Accion == AccionAuditoria.Restaurar));
        }
    }
}
