using GestionAlquileres.Web.Data;
using GestionAlquileres.Web.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GestionAlquileres.Tests.Infraestructura;

/// <summary>
/// Base de datos SQL Server descartable para los tests: nombre único por ejecución, se crea con las
/// migraciones reales y se borra al terminar. Nunca toca la base de desarrollo.
/// El servidor es LocalDB salvo que se indique otro en la variable de entorno GA_TESTS_SERVIDOR.
/// </summary>
public sealed class BaseDeDatosEfimera
{
    public const string PrefijoNombre = "GA_Tests_";

    public BaseDeDatosEfimera(string uso)
    {
        Nombre = $"{PrefijoNombre}{uso}_{Guid.NewGuid():N}";
        var servidor = Environment.GetEnvironmentVariable("GA_TESTS_SERVIDOR") ?? @"(localdb)\MSSQLLocalDB";
        CadenaConexion = $"Server={servidor};Database={Nombre};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=False";
    }

    public string Nombre { get; }

    public string CadenaConexion { get; }

    public DbContextOptions<AppDbContext> Opciones()
    {
        var opciones = new DbContextOptionsBuilder<AppDbContext>();
        Configurar(opciones);
        return opciones.Options;
    }

    /// <summary>Misma configuración que la aplicación (ver PersistenciaExtensions).</summary>
    public DbContextOptionsBuilder Configurar(DbContextOptionsBuilder opciones) => opciones
        .UseSqlServer(CadenaConexion)
        .ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));

    public AppDbContext CrearContexto(IUsuarioActual? usuario = null, TimeProvider? reloj = null) =>
        new(Opciones(), usuario ?? UsuarioDePrueba.Operador, reloj ?? TimeProvider.System);

    public async Task CrearAsync()
    {
        await using var db = CrearContexto();
        await db.Database.MigrateAsync();
    }

    public async Task BorrarAsync()
    {
        // Seguridad: solo se borran bases creadas por los tests.
        if (!Nombre.StartsWith(PrefijoNombre, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Se intentó borrar una base que no es de tests: {Nombre}");
        }

        await using var db = CrearContexto();
        await db.Database.EnsureDeletedAsync();
    }
}

/// <summary>Usuario fijo para los tests que usan el DbContext directamente (auditoría).</summary>
public sealed class UsuarioDePrueba : IUsuarioActual
{
    public static readonly UsuarioDePrueba Operador = new("operador@tests.test");

    public UsuarioDePrueba(string nombre)
    {
        Nombre = nombre;
    }

    public string? Id => "id-" + Nombre;

    public string Nombre { get; }

    public string? DireccionIp => "127.0.0.1";
}
