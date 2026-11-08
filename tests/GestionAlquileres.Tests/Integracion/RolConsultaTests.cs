using GestionAlquileres.Tests.Infraestructura;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Tests.Integracion;

/// <summary>
/// El rol Consulta ve documento, teléfono y email parcialmente ocultos y no ve información de contratos.
/// Se verifica sobre el HTML que llega al navegador: el dato completo no tiene que viajar nunca.
/// Usa los datos de prueba de desarrollo (inquilino Martín Díaz, DNI 35.987.654, alquila Av. Rivadavia 5120).
/// </summary>
[Collection(ColeccionAplicacion.Nombre)]
public class RolConsultaTests
{
    private const string DocumentoCompleto = "35.987.654";
    private const string DocumentoSinFormato = "35987654";
    private const string EmailCompleto = "martin.diaz@correo.test";
    private const string TelefonoCompleto = "11 6555-0202";
    private const string DireccionDelContrato = "Av. Rivadavia 5120";

    private readonly AplicacionFixture _app;

    public RolConsultaTests(AplicacionFixture app)
    {
        _app = app;
    }

    private Task<int> IdInquilinoAsync() =>
        _app.ConsultarAsync(db => db.Inquilinos.Where(i => i.NumeroDocumento == DocumentoSinFormato).Select(i => i.Id).SingleAsync());

    [Fact]
    public async Task Ficha_del_inquilino_para_consulta_sin_datos_completos_ni_contratos()
    {
        var html = await _app.Consulta.GetStringAsync($"/Inquilinos/Detalle/{await IdInquilinoAsync()}");

        Assert.DoesNotContain(DocumentoCompleto, html);
        Assert.DoesNotContain(DocumentoSinFormato, html);
        Assert.DoesNotContain(EmailCompleto, html);
        Assert.DoesNotContain(TelefonoCompleto, html);
        Assert.Contains("**.***.654", html);
        Assert.DoesNotContain(DireccionDelContrato, html);
        Assert.Contains("no está disponible para tu rol", html);
    }

    [Fact]
    public async Task Ficha_del_inquilino_para_operador_con_todos_los_datos()
    {
        var html = await _app.Operador.GetStringAsync($"/Inquilinos/Detalle/{await IdInquilinoAsync()}");

        Assert.Contains(DocumentoCompleto, html);
        Assert.Contains(EmailCompleto, html);
        Assert.Contains(TelefonoCompleto, html);
        Assert.Contains(DireccionDelContrato, html);
    }

    [Fact]
    public async Task Listado_de_inquilinos_para_consulta_sin_contratos_ni_estado()
    {
        // Aunque pida los filtros de contrato en la dirección, se ignoran.
        var html = await _app.Consulta.GetStringAsync("/Inquilinos?Propiedad=Rivadavia&ConContratoVigente=true");

        Assert.DoesNotContain(DocumentoCompleto, html);
        Assert.DoesNotContain(EmailCompleto, html);
        Assert.DoesNotContain(DireccionDelContrato, html);
        Assert.DoesNotContain("Al día", html);
        Assert.DoesNotContain("name=\"ConContratoVigente\"", html);
        Assert.Contains("m***@correo.test", html);
    }

    [Fact]
    public async Task Fichas_de_propietarios_y_garantes_para_consulta_sin_contratos()
    {
        var idPropietario = await _app.ConsultarAsync(db => db.PropiedadesPropietarios.Select(pp => pp.PropietarioId).FirstAsync());
        var idGarante = await _app.ConsultarAsync(db => db.ContratosGarantes.Select(cg => cg.GaranteId).FirstAsync());

        foreach (var url in new[] { $"/Propietarios/Detalle/{idPropietario}", $"/Garantes/Detalle/{idGarante}" })
        {
            var html = await _app.Consulta.GetStringAsync(url);
            Assert.Contains("no está disponible para tu rol", html);
            Assert.DoesNotContain("Inmueble en", html); // detalle de la garantía
        }
    }

    [Fact]
    public async Task Inicio_para_consulta_sin_tablero()
    {
        var html = await _app.Consulta.GetStringAsync("/");

        Assert.DoesNotContain("Contratos que requieren atención", html);
        Assert.DoesNotContain("Moras activas", html);
    }

    [Fact]
    public async Task La_apertura_de_una_ficha_queda_auditada()
    {
        var id = await IdInquilinoAsync();
        var antes = await ContarAperturasAsync(id);

        await _app.Consulta.GetStringAsync($"/Inquilinos/Detalle/{id}");

        Assert.Equal(antes + 1, await ContarAperturasAsync(id));
    }

    private Task<int> ContarAperturasAsync(int idInquilino) =>
        _app.ConsultarAsync(db => db.AuditLogs.CountAsync(a =>
            a.Accion == AccionAuditoria.ConsultaDatosPersonales
            && a.Entidad == nameof(Inquilino)
            && a.EntidadId == idInquilino.ToString()
            && a.UsuarioNombre == AplicacionFixture.EmailDe(Web.Security.Roles.Consulta)));
}
