using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GestionAlquileres.Tests.Infraestructura;
using GestionAlquileres.Web.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Tests.Integracion;

/// <summary>Contratos (alta, PDF adjuntos, rescisión) y asistente, contra la aplicación real.</summary>
[Collection(ColeccionAplicacion.Nombre)]
public class ContratosTests
{
    private const string FormularioContrato = "/Administracion/Contratos/Crear";

    private readonly AplicacionFixture _app;

    public ContratosTests(AplicacionFixture app)
    {
        _app = app;
    }

    private async Task<Dictionary<string, string>> CamposContratoAsync(int propiedadId)
    {
        var inquilinoId = await _app.ConsultarAsync(db => db.Inquilinos.OrderBy(i => i.Id).Select(i => i.Id).FirstAsync());
        var indiceId = await _app.ConsultarAsync(db => db.Indices.Where(i => i.Codigo == "IPC").Select(i => i.Id).FirstAsync());
        return new Dictionary<string, string>
        {
            ["PropiedadId"] = propiedadId.ToString(),
            ["InquilinoTitularId"] = inquilinoId.ToString(),
            ["FechaInicio"] = "2030-01-01",
            ["FechaFin"] = "2031-12-31",
            ["Moneda"] = nameof(Moneda.Ars),
            ["MontoInicial"] = "650000,50",
            ["DiaVencimiento"] = "10",
            ["TipoAjuste"] = nameof(TipoAjuste.Indice),
            ["IndiceId"] = indiceId.ToString(),
            ["PeriodicidadMeses"] = "3",
        };
    }

    private Task<int> PropiedadDisponibleAsync() =>
        _app.ConsultarAsync(db => db.Propiedades
            .Where(p => p.Activa && !p.Contratos.Any(c => c.Estado == EstadoContrato.Vigente))
            .OrderByDescending(p => p.Id)
            .Select(p => p.Id)
            .FirstAsync());

    private static async Task<HttpResponseMessage> SubirPdfAsync(HttpClient cliente, int contratoId, byte[] contenido)
    {
        var parte = new ByteArrayContent(contenido);
        parte.Headers.ContentType = new MediaTypeHeaderValue("application/pdf"); // lo que diga el navegador no importa
        var formulario = new MultipartFormDataContent
        {
            { new StringContent(await AplicacionFixture.ObtenerTokenAsync(cliente, $"/Administracion/Contratos/Detalle/{contratoId}")), "__RequestVerificationToken" },
            { new StringContent("Contrato firmado"), "descripcion" },
            { parte, "archivo", "contrato.pdf" },
        };
        return await cliente.PostAsync($"/Administracion/Contratos/SubirDocumento/{contratoId}", formulario);
    }

    [Fact]
    public async Task El_administrador_crea_un_contrato_adjunta_el_pdf_y_lo_rescinde()
    {
        var propiedadId = await PropiedadDisponibleAsync();

        // Alta: el monto actual y el historial nacen juntos.
        var alta = await AplicacionFixture.EnviarFormularioAsync(_app.Administrador, FormularioContrato, FormularioContrato,
            await CamposContratoAsync(propiedadId));
        Assert.Equal(HttpStatusCode.Redirect, alta.StatusCode);
        var contratoId = int.Parse(alta.Headers.Location!.ToString().Split('/')[^1]);

        var contrato = await _app.ConsultarAsync(db => db.Contratos
            .Include(c => c.HistorialMontos).Include(c => c.ConfiguracionAjuste).Include(c => c.Inquilinos)
            .FirstAsync(c => c.Id == contratoId));
        Assert.Equal(650000.50m, contrato.MontoActual);
        Assert.Single(contrato.HistorialMontos);
        Assert.Single(contrato.Inquilinos, i => i.EsTitular);
        Assert.Equal(new DateOnly(2030, 4, 1), contrato.ConfiguracionAjuste!.ProximaFechaAjuste);

        // PDF: se acepta por su contenido y se descarga como adjunto.
        var subida = await SubirPdfAsync(_app.Administrador, contratoId, Encoding.ASCII.GetBytes("%PDF-1.7\n%prueba\n"));
        Assert.Equal(HttpStatusCode.Redirect, subida.StatusCode);
        var documentoId = await _app.ConsultarAsync(db => db.DocumentosContratos.Where(d => d.ContratoId == contratoId).Select(d => d.Id).SingleAsync());

        var descarga = await _app.Administrador.GetAsync($"/Administracion/Contratos/Documento/{documentoId}");
        Assert.Equal(HttpStatusCode.OK, descarga.StatusCode);
        Assert.Equal("application/pdf", descarga.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", descarga.Content.Headers.ContentDisposition?.DispositionType);

        // Un archivo que no es PDF se rechaza aunque se llame .pdf.
        await SubirPdfAsync(_app.Administrador, contratoId, Encoding.ASCII.GetBytes("<html>no soy un pdf</html>"));
        Assert.Equal(1, await _app.ConsultarAsync(db => db.DocumentosContratos.CountAsync(d => d.ContratoId == contratoId)));

        // Rescisión con fecha dentro de la vigencia.
        var rescision = await AplicacionFixture.EnviarFormularioAsync(_app.Administrador,
            $"/Administracion/Contratos/Rescindir/{contratoId}", $"/Administracion/Contratos/Rescindir/{contratoId}",
            new Dictionary<string, string> { ["FechaRescision"] = "2030-06-30", ["Motivo"] = "Acuerdo entre las partes" });
        Assert.Equal(HttpStatusCode.Redirect, rescision.StatusCode);
        Assert.Equal(EstadoContrato.Rescindido, await _app.ConsultarAsync(db => db.Contratos.Where(c => c.Id == contratoId).Select(c => c.Estado).FirstAsync()));
    }

    [Fact]
    public async Task No_se_crea_un_contrato_sobre_una_propiedad_con_contrato_vigente()
    {
        var ocupada = await _app.ConsultarAsync(db => db.Contratos
            .Where(c => c.Estado == EstadoContrato.Vigente)
            .Select(c => new { c.PropiedadId, c.FechaInicio })
            .FirstAsync());
        var campos = await CamposContratoAsync(ocupada.PropiedadId);
        campos["FechaInicio"] = ocupada.FechaInicio.ToString("yyyy-MM-dd");
        campos["FechaFin"] = ocupada.FechaInicio.AddYears(1).ToString("yyyy-MM-dd");
        var antes = await _app.ConsultarAsync(db => db.Contratos.CountAsync());

        var respuesta = await AplicacionFixture.EnviarFormularioAsync(_app.Administrador, FormularioContrato, FormularioContrato, campos);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode); // vuelve al formulario con el error
        Assert.Contains("ya tiene un contrato vigente en esas fechas", await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(antes, await _app.ConsultarAsync(db => db.Contratos.CountAsync()));
    }

    [Fact]
    public async Task El_operador_no_crea_contratos_ni_descarga_documentos_aunque_arme_el_pedido_a_mano()
    {
        var antes = await _app.ConsultarAsync(db => db.Contratos.CountAsync());
        var respuesta = await AplicacionFixture.EnviarFormularioAsync(_app.Operador, "/Propiedades", FormularioContrato,
            await CamposContratoAsync(await PropiedadDisponibleAsync()));

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.StartsWith("https://localhost/Cuenta/AccesoDenegado", respuesta.Headers.Location!.ToString());
        Assert.Equal(antes, await _app.ConsultarAsync(db => db.Contratos.CountAsync()));

        var descarga = await _app.Operador.GetAsync("/Administracion/Contratos/Documento/1");
        Assert.StartsWith("https://localhost/Cuenta/AccesoDenegado", descarga.Headers.Location!.ToString());
    }

    private static async Task<JsonElement> PreguntarAsync(HttpClient cliente, string pregunta)
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Post, "/Asistente/Preguntar")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = await AplicacionFixture.ObtenerTokenAsync(cliente, "/Asistente"),
                ["pregunta"] = pregunta,
            }),
        };
        pedido.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var respuesta = await cliente.SendAsync(pedido);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync()).RootElement;
    }

    [Fact]
    public async Task El_asistente_no_envia_preguntas_con_datos_personales()
    {
        var resultado = await PreguntarAsync(_app.Consulta, "¿Qué contratos tiene el DNI 20.345.678?");

        Assert.False(resultado.GetProperty("exito").GetBoolean());
        Assert.StartsWith("Por privacidad, la pregunta no se envió", resultado.GetProperty("texto").GetString());
    }

    [Fact]
    public async Task Sin_clave_de_gemini_el_asistente_avisa_que_no_esta_configurado()
    {
        var resultado = await PreguntarAsync(_app.Operador, "¿Cómo cargo un inquilino?");

        Assert.False(resultado.GetProperty("exito").GetBoolean());
        Assert.Contains("no está configurado", resultado.GetProperty("texto").GetString());
    }
}
