using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace GestionAlquileres.Web.ViewModels.Comun;

/// <summary>
/// Nivel intermedio de la ruta de navegación (breadcrumb). El layout agrega siempre "Inicio" al principio
/// y el título de la página al final; las vistas solo declaran los niveles del medio.
/// Si hay niveles intermedios, el layout muestra además un botón "Volver a {último nivel}".
/// </summary>
public record Miga(string Texto, string Url);

public static class MigasExtensions
{
    private const string Clave = "Migas";

    public static IReadOnlyList<Miga> Migas(this ViewDataDictionary viewData) =>
        viewData[Clave] as IReadOnlyList<Miga> ?? [];

    public static void EstablecerMigas(this ViewDataDictionary viewData, params Miga[] migas) =>
        viewData[Clave] = migas;
}
