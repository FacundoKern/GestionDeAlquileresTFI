using System.Security.Claims;
using GestionAlquileres.Web.Models.Entidades;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace GestionAlquileres.Web.Security;

/// <summary>Agrega al cookie de sesión el nombre para mostrar y la marca de cambio de contraseña obligatorio.</summary>
public sealed class FabricaClaimsUsuario : UserClaimsPrincipalFactory<Usuario, IdentityRole>
{
    public FabricaClaimsUsuario(
        UserManager<Usuario> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> opciones)
        : base(userManager, roleManager, opciones)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Usuario usuario)
    {
        var identidad = await base.GenerateClaimsAsync(usuario);
        identidad.AddClaim(new Claim(ClaimsPropios.NombreCompleto, usuario.NombreCompleto));
        if (usuario.DebeCambiarClave)
        {
            identidad.AddClaim(new Claim(ClaimsPropios.DebeCambiarClave, "true"));
        }

        return identidad;
    }
}
