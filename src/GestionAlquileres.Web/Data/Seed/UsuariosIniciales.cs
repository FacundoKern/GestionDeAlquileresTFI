using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Security;
using Microsoft.AspNetCore.Identity;

namespace GestionAlquileres.Web.Data.Seed;

/// <summary>
/// Crea los usuarios definidos en la sección Seed:Usuarios, uno por rol:
/// <code>
/// "Seed:Usuarios:Administrador:Email"          (appsettings)
/// "Seed:Usuarios:Administrador:NombreCompleto" (appsettings)
/// "Seed:Usuarios:Administrador:Clave"          (User Secrets o variable de entorno, NUNCA en el repo)
/// </code>
/// Si falta la contraseña, el usuario no se crea. Si el usuario ya existe no se modifica.
/// </summary>
internal sealed class UsuariosIniciales
{
    private readonly UserManager<Usuario> _userManager;
    private readonly TimeProvider _reloj;
    private readonly ILogger _logger;

    public UsuariosIniciales(UserManager<Usuario> userManager, TimeProvider reloj, ILogger logger)
    {
        _userManager = userManager;
        _reloj = reloj;
        _logger = logger;
    }

    public async Task CrearAsync(IConfigurationSection seccion, bool exigirCambioDeClave)
    {
        foreach (var rol in Roles.Todos)
        {
            var datos = seccion.GetSection(rol);
            var email = datos["Email"];
            if (string.IsNullOrWhiteSpace(email) || await _userManager.FindByEmailAsync(email) is not null)
            {
                continue;
            }

            var clave = datos["Clave"];
            if (string.IsNullOrWhiteSpace(clave))
            {
                _logger.LogWarning(
                    "No se creó el usuario inicial del rol {Rol}: falta Seed:Usuarios:{Rol}:Clave en User Secrets o variables de entorno",
                    rol, rol);
                continue;
            }

            var usuario = new Usuario
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                NombreCompleto = datos["NombreCompleto"] ?? rol,
                Activo = true,
                DebeCambiarClave = exigirCambioDeClave,
                CreadoEnUtc = _reloj.AhoraUtc(),
            };

            var resultado = await _userManager.CreateAsync(usuario, clave);
            if (!resultado.Succeeded)
            {
                _logger.LogError(
                    "No se pudo crear el usuario inicial del rol {Rol}: {Errores}",
                    rol, string.Join(" ", resultado.Errors.Select(e => e.Description)));
                continue;
            }

            await _userManager.AddToRoleAsync(usuario, rol);
            _logger.LogInformation("Usuario inicial creado para el rol {Rol}", rol);
        }
    }
}
