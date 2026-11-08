using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GestionAlquileres.Web.Data;

/// <summary>
/// Solo para las herramientas de EF en tiempo de diseño (dotnet ef migrations ...), para no depender
/// de la configuración de la app. Usa LocalDB salvo que se defina la variable GA_DESIGN_CONNECTION.
/// </summary>
public class DisenoDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var conexion = Environment.GetEnvironmentVariable("GA_DESIGN_CONNECTION")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=GestionAlquileresTFI;Trusted_Connection=True;TrustServerCertificate=True";

        var opciones = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(conexion)
            .ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
            .Options;

        return new AppDbContext(opciones);
    }
}
