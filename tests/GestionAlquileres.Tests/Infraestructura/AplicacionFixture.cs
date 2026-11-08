using System.Net;
using System.Text.RegularExpressions;
using GestionAlquileres.Web.Data;
using GestionAlquileres.Web.Infrastructure.Configuracion;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GestionAlquileres.Tests.Infraestructura;

/// <summary>
/// La aplicación completa en memoria (WebApplicationFactory) sobre una base efímera con los datos de
/// prueba de desarrollo, y un cliente HTTP con sesión iniciada por cada rol.
/// Los usuarios de los tests se crean acá, con contraseñas generadas: no dependen de User Secrets.
/// </summary>
public sealed partial class AplicacionFixture : IAsyncLifetime
{
    private readonly BaseDeDatosEfimera _base = new("Aplicacion");
    private readonly string _clave = $"Tests#{Guid.NewGuid():N}"[..20] + "Aa1!";
    private WebApplicationFactory<Program> _fabrica = null!;

    /// <summary>Carpeta temporal de fotos de esta ejecución (se borra al final).</summary>
    public string CarpetaFotos { get; } = Path.Combine(Path.GetTempPath(), $"GA_Tests_Fotos_{Guid.NewGuid():N}");

    public HttpClient Anonimo { get; private set; } = null!;

    public HttpClient Administrador { get; private set; } = null!;

    public HttpClient Operador { get; private set; } = null!;

    public HttpClient Consulta { get; private set; } = null!;

    public static string EmailDe(string rol) => $"{rol.ToLowerInvariant()}@tests.test";

    public HttpClient ClienteDe(string rol) => rol switch
    {
        Roles.Administrador => Administrador,
        Roles.Operador => Operador,
        Roles.Consulta => Consulta,
        _ => Anonimo,
    };

    public async Task InitializeAsync()
    {
        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // Development: migraciones automáticas y datos de prueba (appsettings.Development.json).
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:GestionAlquileres", _base.CadenaConexion);

            // Se reemplaza el DbContext por el de la base efímera sin importar qué haya en User Secrets:
            // los tests nunca deben tocar la base de desarrollo.
            builder.ConfigureTestServices(servicios =>
            {
                servicios.RemoveAll<DbContextOptions<AppDbContext>>();
                servicios.AddDbContext<AppDbContext>(opciones => _base.Configurar(opciones));

                // Las fotos que suben los tests van a una carpeta temporal, nunca a la de la aplicación.
                servicios.PostConfigure<FotosOptions>(opciones => opciones.Carpeta = CarpetaFotos);
            });
        });

        // Arranca la aplicación (aplica migraciones y carga los datos de prueba).
        _ = _fabrica.Server;
        await VerificarBaseEfimeraAsync();
        await CrearUsuariosDePruebaAsync();

        Anonimo = CrearCliente();
        Administrador = await IniciarSesionAsync(Roles.Administrador);
        Operador = await IniciarSesionAsync(Roles.Operador);
        Consulta = await IniciarSesionAsync(Roles.Consulta);
    }

    public async Task DisposeAsync()
    {
        await _fabrica.DisposeAsync();
        await _base.BorrarAsync();
        if (Directory.Exists(CarpetaFotos))
        {
            Directory.Delete(CarpetaFotos, recursive: true);
        }
    }

    /// <summary>Consulta la base de la aplicación directamente (para verificar efectos).</summary>
    public async Task<T> ConsultarAsync<T>(Func<AppDbContext, Task<T>> consulta)
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        return await consulta(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    /// <summary>Token antiforgery de un formulario de la página (el token depende del usuario de la sesión).</summary>
    public static async Task<string> ObtenerTokenAsync(HttpClient cliente, string url)
    {
        var html = await cliente.GetStringAsync(url);
        var token = TokenAntiforgery().Match(html);
        Assert.True(token.Success, $"No se encontró el token antiforgery en {url}");
        return WebUtility.HtmlDecode(token.Groups[1].Value);
    }

    public static async Task<HttpResponseMessage> EnviarFormularioAsync(
        HttpClient cliente, string urlFormulario, string urlDestino, IDictionary<string, string> campos)
    {
        var datos = new Dictionary<string, string>(campos)
        {
            ["__RequestVerificationToken"] = await ObtenerTokenAsync(cliente, urlFormulario),
        };
        return await cliente.PostAsync(urlDestino, new FormUrlEncodedContent(datos));
    }

    private HttpClient CrearCliente() => _fabrica.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false, // los tests verifican a dónde redirige cada respuesta
        BaseAddress = new Uri("https://localhost"), // las cookies __Host- exigen HTTPS
        HandleCookies = true,
    });

    private async Task<HttpClient> IniciarSesionAsync(string rol)
    {
        var cliente = CrearCliente();
        var respuesta = await EnviarFormularioAsync(cliente, "/Cuenta/IniciarSesion", "/Cuenta/IniciarSesion",
            new Dictionary<string, string> { ["Email"] = EmailDe(rol), ["Clave"] = _clave, ["Recordarme"] = "false" });

        Assert.True(respuesta.StatusCode == HttpStatusCode.Redirect, $"No se pudo iniciar sesión como {rol}: {respuesta.StatusCode}");
        Assert.Equal("/", respuesta.Headers.Location?.OriginalString);
        return cliente;
    }

    private async Task VerificarBaseEfimeraAsync()
    {
        var nombre = await ConsultarAsync(db => Task.FromResult(db.Database.GetDbConnection().Database));
        if (nombre != _base.Nombre)
        {
            throw new InvalidOperationException($"La aplicación de tests no está usando la base efímera (usa '{nombre}').");
        }
    }

    private async Task CrearUsuariosDePruebaAsync()
    {
        await using var scope = _fabrica.Services.CreateAsyncScope();
        var usuarios = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        foreach (var rol in Roles.Todos)
        {
            var usuario = new Usuario
            {
                UserName = EmailDe(rol),
                Email = EmailDe(rol),
                EmailConfirmed = true,
                NombreCompleto = $"{rol} (tests)",
                Activo = true,
                CreadoEnUtc = DateTime.UtcNow,
            };
            var resultado = await usuarios.CreateAsync(usuario, _clave);
            Assert.True(resultado.Succeeded, string.Join(" ", resultado.Errors.Select(e => e.Description)));
            await usuarios.AddToRoleAsync(usuario, rol);
        }
    }

    [GeneratedRegex(@"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]+)""")]
    private static partial Regex TokenAntiforgery();
}

[CollectionDefinition(Nombre)]
public sealed class ColeccionAplicacion : ICollectionFixture<AplicacionFixture>
{
    public const string Nombre = "Aplicación";
}
