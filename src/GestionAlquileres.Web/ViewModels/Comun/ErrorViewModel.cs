namespace GestionAlquileres.Web.ViewModels.Comun;

public class ErrorViewModel
{
    public string? IdSolicitud { get; init; }

    public int CodigoEstado { get; init; } = 500;

    public string Titulo { get; init; } = "Ocurrió un error";

    public string Mensaje { get; init; } =
        "No pudimos completar la operación. Si el problema persiste, comunicate con el administrador.";

    public static ErrorViewModel ParaCodigo(int codigo) => codigo switch
    {
        404 => new() { CodigoEstado = 404, Titulo = "Página no encontrada", Mensaje = "La página que buscás no existe o fue movida." },
        403 => new() { CodigoEstado = 403, Titulo = "Acceso denegado", Mensaje = "No tenés permisos para acceder a esta sección." },
        429 => new() { CodigoEstado = 429, Titulo = "Demasiados intentos", Mensaje = "Hiciste demasiadas solicitudes seguidas. Esperá un minuto y volvé a intentar." },
        _ => new() { CodigoEstado = codigo },
    };
}
