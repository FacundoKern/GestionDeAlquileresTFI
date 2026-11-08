using System.Security.Claims;

namespace GestionAlquileres.Web.Security;

/// <summary>Quién está operando, para auditoría. Fuera de un request (seed, tareas) es "sistema".</summary>
public interface IUsuarioActual
{
    string? Id { get; }

    string Nombre { get; }

    string? DireccionIp { get; }
}

public sealed class UsuarioActualHttp : IUsuarioActual
{
    public const string NombreSistema = "sistema";
    public const string NombreAnonimo = "anónimo";

    private readonly IHttpContextAccessor _accesor;

    public UsuarioActualHttp(IHttpContextAccessor accesor)
    {
        _accesor = accesor;
    }

    private ClaimsPrincipal? Usuario => _accesor.HttpContext?.User;

    public string? Id => Usuario?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string Nombre => _accesor.HttpContext is null
        ? NombreSistema
        : Usuario?.Identity is { IsAuthenticated: true, Name: { } nombre } ? nombre : NombreAnonimo;

    public string? DireccionIp => _accesor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}

/// <summary>Usuario fijo para contextos sin HTTP (diseño, seed, tests).</summary>
public sealed class UsuarioSistema : IUsuarioActual
{
    public static readonly UsuarioSistema Instancia = new();

    public string? Id => null;

    public string Nombre => UsuarioActualHttp.NombreSistema;

    public string? DireccionIp => null;
}
