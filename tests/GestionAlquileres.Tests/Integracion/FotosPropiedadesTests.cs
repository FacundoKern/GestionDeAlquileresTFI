using System.Net;
using System.Net.Http.Headers;
using System.Text;
using GestionAlquileres.Tests.Infraestructura;
using GestionAlquileres.Tests.Unidad;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Tests.Integracion;

/// <summary>Fotos de propiedades contra la aplicación real: subida, permisos, archivos fuera de wwwroot y borrado.</summary>
[Collection(ColeccionAplicacion.Nombre)]
public class FotosPropiedadesTests
{
    private readonly AplicacionFixture _app;

    public FotosPropiedadesTests(AplicacionFixture app)
    {
        _app = app;
    }

    /// <summary>Cada test usa una propiedad distinta de los datos de prueba, para no depender del orden.</summary>
    private Task<int> PropiedadAsync(int posicion) =>
        _app.ConsultarAsync(db => db.Propiedades.OrderBy(p => p.Id).Skip(posicion).Select(p => p.Id).FirstAsync());

    private Task<List<(int Id, bool EsPrincipal, string Archivo)>> FotosAsync(int propiedadId) =>
        _app.ConsultarAsync(async db => (await db.FotosPropiedades
                .Where(f => f.PropiedadId == propiedadId)
                .OrderBy(f => f.Orden)
                .Select(f => new { f.Id, f.EsPrincipal, f.Archivo })
                .ToListAsync())
            .Select(f => (f.Id, f.EsPrincipal, f.Archivo))
            .ToList());

    private static async Task<HttpResponseMessage> SubirAsync(HttpClient cliente, int propiedadId, params (string Nombre, byte[] Contenido)[] archivos)
    {
        var formulario = new MultipartFormDataContent
        {
            { new StringContent(await AplicacionFixture.ObtenerTokenAsync(cliente, "/Propiedades")), "__RequestVerificationToken" },
        };
        foreach (var (nombre, contenido) in archivos)
        {
            var parte = new ByteArrayContent(contenido);
            parte.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg"); // lo que diga el navegador no importa
            formulario.Add(parte, "archivos", nombre);
        }

        return await cliente.PostAsync($"/FotosPropiedades/Subir/{propiedadId}", formulario);
    }

    [Fact]
    public async Task El_operador_sube_fotos_y_todos_los_roles_las_ven()
    {
        var propiedadId = await PropiedadAsync(0);

        var respuesta = await SubirAsync(_app.Operador, propiedadId,
            ("living.jpg", ProcesadorImagenesTests.CrearImagen(2400, 1600)),
            ("cocina.png", ProcesadorImagenesTests.CrearImagen(800, 600, SkiaSharp.SKEncodedImageFormat.Png)));

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.EndsWith($"/Propiedades/Detalle/{propiedadId}#fotos", respuesta.Headers.Location!.ToString());

        var fotos = await FotosAsync(propiedadId);
        Assert.Equal(2, fotos.Count);
        Assert.Single(fotos, f => f.EsPrincipal);
        Assert.True(File.Exists(Path.Combine(_app.CarpetaFotos, fotos[0].Archivo + ".jpg")));
        Assert.True(File.Exists(Path.Combine(_app.CarpetaFotos, fotos[0].Archivo + "_m.jpg")));

        foreach (var cliente in new[] { _app.Administrador, _app.Operador, _app.Consulta })
        {
            var foto = await cliente.GetAsync($"/FotosPropiedades/Ver/{fotos[0].Id}");
            Assert.Equal(HttpStatusCode.OK, foto.StatusCode);
            Assert.Equal("image/jpeg", foto.Content.Headers.ContentType?.MediaType);
        }

        var ficha = await _app.Consulta.GetStringAsync($"/Propiedades/Detalle/{propiedadId}");
        Assert.Contains($"/FotosPropiedades/Ver/{fotos[0].Id}?miniatura=True", ficha);
        Assert.DoesNotContain("Subir fotos", ficha); // Consulta no ve el formulario
    }

    [Fact]
    public async Task Un_archivo_que_no_es_imagen_se_rechaza_con_un_mensaje()
    {
        var propiedadId = await PropiedadAsync(1);

        var respuesta = await SubirAsync(_app.Operador, propiedadId, ("factura.jpg", Encoding.UTF8.GetBytes("no soy una foto")));

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Empty(await FotosAsync(propiedadId));
        var ficha = await _app.Operador.GetStringAsync(respuesta.Headers.Location!);
        Assert.Contains("«factura.jpg» no es una imagen válida.", ficha);
    }

    [Fact]
    public async Task Consulta_no_puede_subir_fotos()
    {
        var propiedadId = await PropiedadAsync(2);

        var respuesta = await SubirAsync(_app.Consulta, propiedadId, ("foto.jpg", ProcesadorImagenesTests.CrearImagen(100, 100)));

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.StartsWith("https://localhost/Cuenta/AccesoDenegado", respuesta.Headers.Location!.ToString());
        Assert.Empty(await FotosAsync(propiedadId));
    }

    [Fact]
    public async Task Sin_sesion_las_fotos_no_se_pueden_ver()
    {
        var propiedadId = await PropiedadAsync(3);
        await SubirAsync(_app.Operador, propiedadId, ("foto.jpg", ProcesadorImagenesTests.CrearImagen(100, 100)));
        var fotoId = (await FotosAsync(propiedadId))[0].Id;

        var respuesta = await _app.Anonimo.GetAsync($"/FotosPropiedades/Ver/{fotoId}");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.StartsWith("https://localhost/Cuenta/IniciarSesion", respuesta.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Al_eliminar_la_principal_deja_de_verse_y_la_siguiente_pasa_a_ser_principal()
    {
        var propiedadId = await PropiedadAsync(4);
        await SubirAsync(_app.Operador, propiedadId,
            ("a.jpg", ProcesadorImagenesTests.CrearImagen(100, 100)),
            ("b.jpg", ProcesadorImagenesTests.CrearImagen(100, 100)));
        var antes = await FotosAsync(propiedadId);
        var principal = antes.Single(f => f.EsPrincipal);

        await AplicacionFixture.EnviarFormularioAsync(_app.Administrador, $"/FotosPropiedades/Eliminar/{principal.Id}",
            $"/FotosPropiedades/Eliminar/{principal.Id}",
            new Dictionary<string, string> { ["rowVersion"] = string.Empty });

        Assert.Equal(HttpStatusCode.NotFound, (await _app.Administrador.GetAsync($"/FotosPropiedades/Ver/{principal.Id}")).StatusCode);
        var despues = await FotosAsync(propiedadId);
        var restante = Assert.Single(despues);
        Assert.True(restante.EsPrincipal);
        Assert.True(File.Exists(Path.Combine(_app.CarpetaFotos, principal.Archivo + ".jpg"))); // borrado lógico: el archivo queda
    }

    [Fact]
    public async Task Un_numero_de_foto_inexistente_devuelve_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _app.Consulta.GetAsync("/FotosPropiedades/Ver/999999")).StatusCode);
    }
}
