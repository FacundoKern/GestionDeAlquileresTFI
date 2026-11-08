using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Models.Enums;
using GestionAlquileres.Web.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Web.Data.Seed;

/// <summary>
/// Inicialización al arrancar la aplicación:
/// - Migraciones automáticas solo si BaseDeDatos:AplicarMigracionesAlIniciar = true (desarrollo).
/// - Datos base en todos los entornos (idempotente): roles, índices ICL/IPC/Casa Propia, numerador de recibos.
/// - Usuarios iniciales definidos en Seed:Usuarios (las contraseñas SOLO desde User Secrets / variables de entorno).
/// - Datos de prueba solo en Development y si Seed:DatosDePrueba = true.
/// </summary>
public static class InicializadorBaseDeDatos
{
    public static async Task InicializarBaseDeDatosAsync(this WebApplication app, CancellationToken ct = default)
    {
        var configuracion = app.Configuration;
        if (!configuracion.GetValue("BaseDeDatos:InicializarAlIniciar", true))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var servicios = scope.ServiceProvider;
        var logger = servicios.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(InicializadorBaseDeDatos));
        var db = servicios.GetRequiredService<AppDbContext>();

        if (configuracion.GetValue<bool>("BaseDeDatos:AplicarMigracionesAlIniciar"))
        {
            logger.LogInformation("Aplicando migraciones pendientes");
            await db.Database.MigrateAsync(ct);
        }

        await CargarDatosBaseAsync(db, servicios.GetRequiredService<RoleManager<IdentityRole>>(), ct);

        await new UsuariosIniciales(
                servicios.GetRequiredService<UserManager<Usuario>>(),
                servicios.GetRequiredService<TimeProvider>(),
                logger)
            .CrearAsync(configuracion.GetSection("Seed:Usuarios"), exigirCambioDeClave: !app.Environment.IsDevelopment());

        if (app.Environment.IsDevelopment() && configuracion.GetValue<bool>("Seed:DatosDePrueba"))
        {
            var datosDePrueba = new DatosDePrueba(db, servicios.GetRequiredService<TimeProvider>());
            if (await datosDePrueba.CargarAsync(ct))
            {
                logger.LogInformation("Datos de prueba cargados");
            }

            if (await datosDePrueba.CargarDatosEmpresaAsync(ct))
            {
                logger.LogInformation("Datos de ejemplo de la inmobiliaria cargados");
            }
        }
    }

    private static async Task CargarDatosBaseAsync(AppDbContext db, RoleManager<IdentityRole> roles, CancellationToken ct)
    {
        foreach (var rol in Roles.Todos)
        {
            if (!await roles.RoleExistsAsync(rol))
            {
                await roles.CreateAsync(new IdentityRole(rol));
            }
        }

        var indicesBase = new[]
        {
            new Indice
            {
                Codigo = "ICL",
                Nombre = "Índice para Contratos de Locación (ICL)",
                Descripcion = "Publicado diariamente por el BCRA.",
                Periodicidad = PeriodicidadIndice.Diaria,
            },
            new Indice
            {
                Codigo = "IPC",
                Nombre = "Índice de Precios al Consumidor (IPC)",
                Descripcion = "Nivel general publicado mensualmente por el INDEC. Se carga el nivel del índice, no la variación.",
                Periodicidad = PeriodicidadIndice.Mensual,
            },
            new Indice
            {
                Codigo = "CASA_PROPIA",
                Nombre = "Coeficiente Casa Propia",
                Descripcion = "Publicado mensualmente por el Ministerio de Desarrollo Territorial y Hábitat.",
                Periodicidad = PeriodicidadIndice.Mensual,
            },
        };

        var existentes = await db.Indices.IgnoreQueryFilters().Select(i => i.Codigo).ToListAsync(ct);
        db.Indices.AddRange(indicesBase.Where(i => !existentes.Contains(i.Codigo)));

        if (!await db.Numeradores.AnyAsync(n => n.Clave == Numerador.Recibos, ct))
        {
            db.Numeradores.Add(new Numerador { Clave = Numerador.Recibos, UltimoValor = 0 });
        }

        await db.SaveChangesAsync(ct);
    }
}
