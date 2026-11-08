using GestionAlquileres.Web.Data;
using GestionAlquileres.Web.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GestionAlquileres.Web.Infrastructure.Configuracion;

public static class PersistenciaExtensions
{
    public const string NombreConexion = "GestionAlquileres";

    /// <summary>
    /// Registra el DbContext. La cadena de conexión NO está en el repo: se toma de User Secrets
    /// (desarrollo) o de la variable de entorno ConnectionStrings__GestionAlquileres.
    /// </summary>
    public static IServiceCollection AgregarPersistencia(this IServiceCollection services, IConfiguration configuracion)
    {
        var conexion = configuracion.GetConnectionString(NombreConexion);
        if (string.IsNullOrWhiteSpace(conexion))
        {
            throw new InvalidOperationException(
                $"Falta la cadena de conexión '{NombreConexion}'. Configurala con " +
                $"'dotnet user-secrets set \"ConnectionStrings:{NombreConexion}\" \"...\"' " +
                $"o con la variable de entorno ConnectionStrings__{NombreConexion}. Ver README.");
        }

        services.AddHttpContextAccessor();
        services.AddScoped<IUsuarioActual, UsuarioActualHttp>();

        services.AddDbContext<AppDbContext>(opciones => opciones
            .UseSqlServer(conexion)
            // Las entidades dependientes nunca quedan apuntando a un principal borrado lógicamente:
            // las reglas de negocio impiden borrar lo que tiene historial (se desactiva en su lugar).
            .ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)));

        return services;
    }
}
