using GestionAlquileres.Web.Data;
using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Models.Enums;
using GestionAlquileres.Web.Security;
using GestionAlquileres.Web.Services.Auditoria;
using GestionAlquileres.Web.Services.Comun;
using GestionAlquileres.Web.ViewModels.Usuarios;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Web.Services.Usuarios;

/// <summary>
/// Gestión de usuarios del sistema (solo Administrador). Cada usuario tiene un único rol.
/// Reglas de seguridad:
/// - Siempre tiene que quedar al menos un Administrador activo.
/// - El administrador no puede cambiarse su propio rol ni desactivarse (evita quedarse afuera por error).
/// - Desactivar, cambiar el rol o el email, o resetear la contraseña invalida las sesiones abiertas del
///   usuario (sello de seguridad nuevo; el cookie se revalida cada 5 minutos).
/// - Usuario no se audita automáticamente (tiene el hash de la contraseña): cada operación registra un
///   evento explícito, sin contraseñas.
/// </summary>
public interface IUsuariosService
{
    Task RegistrarIngresoAsync(string usuarioId, CancellationToken ct = default);

    Task<IReadOnlyList<UsuarioFilaViewModel>> ListarAsync(CancellationToken ct = default);

    Task<UsuarioEditarViewModel?> ObtenerParaEditarAsync(string id, CancellationToken ct = default);

    /// <summary>Completa la información de solo lectura del formulario (para volver a mostrarlo con errores).</summary>
    Task CompletarEstadoAsync(UsuarioEditarViewModel modelo, CancellationToken ct = default);

    Task<string?> ObtenerNombreAsync(string id, CancellationToken ct = default);

    Task<ResultadoOperacion> CrearAsync(UsuarioCrearViewModel modelo, CancellationToken ct = default);

    Task<ResultadoOperacion> ActualizarAsync(string id, UsuarioEditarViewModel modelo, CancellationToken ct = default);

    Task<ResultadoOperacion> CambiarActivoAsync(string id, bool activo, CancellationToken ct = default);

    Task<ResultadoOperacion> DesbloquearAsync(string id, CancellationToken ct = default);

    Task<ResultadoOperacion> ResetearClaveAsync(string id, string clave, CancellationToken ct = default);
}

public sealed class UsuariosService : IUsuariosService
{
    private readonly AppDbContext _db;
    private readonly UserManager<Usuario> _userManager;
    private readonly IAuditoriaService _auditoria;
    private readonly IUsuarioActual _usuarioActual;
    private readonly TimeProvider _reloj;

    public UsuariosService(
        AppDbContext db,
        UserManager<Usuario> userManager,
        IAuditoriaService auditoria,
        IUsuarioActual usuarioActual,
        TimeProvider reloj)
    {
        _db = db;
        _userManager = userManager;
        _auditoria = auditoria;
        _usuarioActual = usuarioActual;
        _reloj = reloj;
    }

    /// <summary>
    /// Actualiza la fecha de último ingreso con un UPDATE directo. No usa UserManager.UpdateAsync
    /// porque ese método valida el ConcurrencyStamp: dos inicios de sesión simultáneos del mismo
    /// usuario (doble clic, dos pestañas) chocarían por un dato que no lo amerita.
    /// </summary>
    public Task RegistrarIngresoAsync(string usuarioId, CancellationToken ct = default)
    {
        var ahora = _reloj.AhoraUtc();
        return _db.Users
            .Where(u => u.Id == usuarioId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.UltimoIngresoUtc, ahora), ct);
    }

    public async Task<IReadOnlyList<UsuarioFilaViewModel>> ListarAsync(CancellationToken ct = default)
    {
        // Son pocos usuarios (la inmobiliaria y algún empleado): sin paginación.
        var usuarios = await _db.Users.AsNoTracking().ToListAsync(ct);
        var roles = await RolesPorUsuarioAsync(ct);

        return usuarios
            .OrderByDescending(u => u.Activo)
            .ThenBy(u => u.NombreCompleto)
            .Select(u => new UsuarioFilaViewModel(
                u.Id,
                u.NombreCompleto,
                u.Email ?? string.Empty,
                roles.GetValueOrDefault(u.Id) ?? "Sin rol",
                Estado(u),
                u.Id == _usuarioActual.Id))
            .ToList();
    }

    public async Task<UsuarioEditarViewModel?> ObtenerParaEditarAsync(string id, CancellationToken ct = default)
    {
        var usuario = await _userManager.FindByIdAsync(id);
        if (usuario is null)
        {
            return null;
        }

        return new UsuarioEditarViewModel
        {
            Id = usuario.Id,
            NombreCompleto = usuario.NombreCompleto,
            Email = usuario.Email ?? string.Empty,
            Rol = await RolDeAsync(usuario) ?? string.Empty,
            ConcurrencyStamp = usuario.ConcurrencyStamp,
            Estado = Estado(usuario),
            EsUstedMismo = usuario.Id == _usuarioActual.Id,
        };
    }

    public async Task CompletarEstadoAsync(UsuarioEditarViewModel modelo, CancellationToken ct = default)
    {
        var usuario = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == modelo.Id, ct);
        modelo.Estado = usuario is null ? null : Estado(usuario);
        modelo.EsUstedMismo = modelo.Id == _usuarioActual.Id;
    }

    public Task<string?> ObtenerNombreAsync(string id, CancellationToken ct = default) =>
        _db.Users.Where(u => u.Id == id).Select(u => u.NombreCompleto).FirstOrDefaultAsync(ct);

    public async Task<ResultadoOperacion> CrearAsync(UsuarioCrearViewModel modelo, CancellationToken ct = default)
    {
        if (!Roles.Todos.Contains(modelo.Rol))
        {
            return ResultadoOperacion.Fallo("Elegí un rol válido.", nameof(modelo.Rol));
        }

        var email = modelo.Email.Trim();
        var usuario = new Usuario
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            NombreCompleto = modelo.NombreCompleto.Trim(),
            Activo = true,
            DebeCambiarClave = true, // la contraseña la eligió el administrador: el usuario la cambia al entrar
            CreadoEnUtc = _reloj.AhoraUtc(),
        };

        // Alta y rol juntos: nunca queda un usuario sin rol.
        await using (var transaccion = await _db.Database.BeginTransactionAsync(ct))
        {
            var resultado = await _userManager.CreateAsync(usuario, modelo.Clave);
            if (!resultado.Succeeded)
            {
                return Traducir(resultado);
            }

            resultado = await _userManager.AddToRoleAsync(usuario, modelo.Rol);
            if (!resultado.Succeeded)
            {
                return Traducir(resultado);
            }

            await transaccion.CommitAsync(ct);
        }

        await RegistrarAsync(usuario.Id, $"Alta de usuario {email} con rol {modelo.Rol}", ct);
        return ResultadoOperacion.Ok();
    }

    public async Task<ResultadoOperacion> ActualizarAsync(string id, UsuarioEditarViewModel modelo, CancellationToken ct = default)
    {
        var usuario = await _userManager.FindByIdAsync(id);
        if (usuario is null)
        {
            return ResultadoOperacion.NoEncontrado();
        }

        // Otro administrador (o el propio usuario, al fallar el ingreso) lo modificó después de abrir el formulario.
        // Se compara a mano: UserStore.UpdateAsync vuelve a adjuntar la entidad y pisa el valor original del token.
        if (!string.IsNullOrEmpty(modelo.ConcurrencyStamp) && modelo.ConcurrencyStamp != usuario.ConcurrencyStamp)
        {
            return ResultadoOperacion.ConflictoConcurrencia();
        }

        if (!Roles.Todos.Contains(modelo.Rol))
        {
            return ResultadoOperacion.Fallo("Elegí un rol válido.", nameof(modelo.Rol));
        }

        var rolActual = await RolDeAsync(usuario);
        var cambiaRol = rolActual != modelo.Rol;
        if (cambiaRol && id == _usuarioActual.Id)
        {
            return ResultadoOperacion.Fallo(
                "No podés cambiar tu propio rol. Si hace falta, pedíselo a otro administrador.", nameof(modelo.Rol));
        }

        if (cambiaRol && await EsUnicoAdministradorActivoAsync(usuario))
        {
            return ResultadoOperacion.Fallo(
                "Es el único administrador activo: primero asigná el rol Administrador a otro usuario.", nameof(modelo.Rol));
        }

        var email = modelo.Email.Trim();
        var nombre = modelo.NombreCompleto.Trim();
        var cambios = new List<string>();
        if (usuario.NombreCompleto != nombre)
        {
            cambios.Add($"nombre: {usuario.NombreCompleto} → {nombre}");
        }

        var cambiaEmail = !string.Equals(usuario.Email, email, StringComparison.OrdinalIgnoreCase);
        if (cambiaEmail)
        {
            cambios.Add($"email: {usuario.Email} → {email}");
        }

        if (cambiaRol)
        {
            cambios.Add($"rol: {rolActual ?? "sin rol"} → {modelo.Rol}");
        }

        if (cambios.Count == 0)
        {
            return ResultadoOperacion.Ok();
        }

        await using (var transaccion = await _db.Database.BeginTransactionAsync(ct))
        {
            usuario.NombreCompleto = nombre;
            usuario.Email = email;
            usuario.UserName = email;
            var resultado = await _userManager.UpdateAsync(usuario);
            if (!resultado.Succeeded)
            {
                return Traducir(resultado);
            }

            if (cambiaRol)
            {
                if (rolActual is not null)
                {
                    resultado = await _userManager.RemoveFromRoleAsync(usuario, rolActual);
                }

                if (resultado.Succeeded)
                {
                    resultado = await _userManager.AddToRoleAsync(usuario, modelo.Rol);
                }

                if (!resultado.Succeeded)
                {
                    return Traducir(resultado);
                }
            }

            if (cambiaRol || cambiaEmail)
            {
                await _userManager.UpdateSecurityStampAsync(usuario); // cierra las sesiones abiertas con el rol o email anterior
            }

            await transaccion.CommitAsync(ct);
        }

        await RegistrarAsync(id, "Cambio de datos: " + string.Join("; ", cambios), ct);
        return ResultadoOperacion.Ok();
    }

    public async Task<ResultadoOperacion> CambiarActivoAsync(string id, bool activo, CancellationToken ct = default)
    {
        var usuario = await _userManager.FindByIdAsync(id);
        if (usuario is null)
        {
            return ResultadoOperacion.NoEncontrado();
        }

        if (usuario.Activo == activo)
        {
            return ResultadoOperacion.Ok();
        }

        if (!activo && id == _usuarioActual.Id)
        {
            return ResultadoOperacion.Fallo("No podés desactivar tu propia cuenta.");
        }

        if (!activo && await EsUnicoAdministradorActivoAsync(usuario))
        {
            return ResultadoOperacion.Fallo("Es el único administrador activo: no se puede desactivar.");
        }

        usuario.Activo = activo;
        var resultado = await _userManager.UpdateAsync(usuario);
        if (resultado.Succeeded && !activo)
        {
            resultado = await _userManager.UpdateSecurityStampAsync(usuario); // cierra sus sesiones abiertas
        }

        if (!resultado.Succeeded)
        {
            return Traducir(resultado);
        }

        await RegistrarAsync(id, activo ? "Activación de la cuenta" : "Desactivación de la cuenta", ct);
        return ResultadoOperacion.Ok();
    }

    public async Task<ResultadoOperacion> DesbloquearAsync(string id, CancellationToken ct = default)
    {
        var usuario = await _userManager.FindByIdAsync(id);
        if (usuario is null)
        {
            return ResultadoOperacion.NoEncontrado();
        }

        var resultado = await _userManager.SetLockoutEndDateAsync(usuario, null);
        if (resultado.Succeeded)
        {
            resultado = await _userManager.ResetAccessFailedCountAsync(usuario);
        }

        if (!resultado.Succeeded)
        {
            return Traducir(resultado);
        }

        await RegistrarAsync(id, "Desbloqueo de la cuenta", ct);
        return ResultadoOperacion.Ok();
    }

    public async Task<ResultadoOperacion> ResetearClaveAsync(string id, string clave, CancellationToken ct = default)
    {
        var usuario = await _userManager.FindByIdAsync(id);
        if (usuario is null)
        {
            return ResultadoOperacion.NoEncontrado();
        }

        if (id == _usuarioActual.Id)
        {
            return ResultadoOperacion.Fallo("Para cambiar tu propia contraseña usá \"Cambiar contraseña\" en el menú de tu cuenta.");
        }

        await using (var transaccion = await _db.Database.BeginTransactionAsync(ct))
        {
            // ResetPasswordAsync valida la política de contraseñas y cambia el sello de seguridad (cierra sus sesiones).
            var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
            var resultado = await _userManager.ResetPasswordAsync(usuario, token, clave);
            if (!resultado.Succeeded)
            {
                return Traducir(resultado, campoClave: nameof(ResetearClaveViewModel.Clave));
            }

            // Si estaba bloqueado por intentos fallidos, el reseteo también lo desbloquea.
            usuario.DebeCambiarClave = true;
            usuario.LockoutEnd = null;
            usuario.AccessFailedCount = 0;
            resultado = await _userManager.UpdateAsync(usuario);
            if (!resultado.Succeeded)
            {
                return Traducir(resultado);
            }

            await transaccion.CommitAsync(ct);
        }

        await RegistrarAsync(id, "Reseteo de contraseña (deberá cambiarla al ingresar)", ct);
        return ResultadoOperacion.Ok();
    }

    private UsuarioEstadoViewModel Estado(Usuario usuario)
    {
        var bloqueadoHasta = usuario.LockoutEnd is { } fin && fin > _reloj.GetUtcNow() ? fin.UtcDateTime : (DateTime?)null;
        return new UsuarioEstadoViewModel(usuario.Activo, bloqueadoHasta, usuario.DebeCambiarClave, usuario.CreadoEnUtc, usuario.UltimoIngresoUtc);
    }

    private async Task<string?> RolDeAsync(Usuario usuario) => (await _userManager.GetRolesAsync(usuario)).FirstOrDefault();

    private async Task<Dictionary<string, string>> RolesPorUsuarioAsync(CancellationToken ct)
    {
        var asignaciones = await (
                from ur in _db.UserRoles
                join r in _db.Roles on ur.RoleId equals r.Id
                select new { ur.UserId, r.Name })
            .ToListAsync(ct);

        return asignaciones
            .GroupBy(a => a.UserId)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(a => a.Name).Order()));
    }

    private async Task<bool> EsUnicoAdministradorActivoAsync(Usuario usuario)
    {
        var administradores = await _userManager.GetUsersInRoleAsync(Roles.Administrador);
        return usuario.Activo
            && administradores.Any(a => a.Id == usuario.Id)
            && administradores.Count(a => a.Activo) == 1;
    }

    private Task RegistrarAsync(string usuarioId, string detalle, CancellationToken ct) =>
        _auditoria.RegistrarEventoAsync(AccionAuditoria.GestionUsuario, nameof(Usuario), usuarioId, detalle, ct);

    /// <summary>Convierte los errores de Identity (ya en español) en un resultado asociado al campo que corresponde.</summary>
    private static ResultadoOperacion Traducir(IdentityResult resultado, string campoClave = nameof(ClaveTemporalViewModel.Clave))
    {
        var errores = resultado.Errors.ToList();

        // El email también es el nombre de usuario: el duplicado se informa una sola vez.
        if (errores.Any(e => e.Code == nameof(IdentityErrorDescriber.DuplicateEmail)))
        {
            errores.RemoveAll(e => e.Code == nameof(IdentityErrorDescriber.DuplicateUserName));
        }

        var primero = errores.FirstOrDefault()?.Code ?? string.Empty;
        if (primero == nameof(IdentityErrorDescriber.ConcurrencyFailure))
        {
            return ResultadoOperacion.ConflictoConcurrencia();
        }

        string? campo = null;
        if (primero.StartsWith("Password", StringComparison.Ordinal))
        {
            campo = campoClave;
        }
        else if (primero.Contains("Email", StringComparison.Ordinal) || primero.Contains("UserName", StringComparison.Ordinal))
        {
            campo = nameof(UsuarioCrearViewModel.Email);
        }

        return ResultadoOperacion.Fallo(string.Join(" ", errores.Select(e => e.Description)), campo);
    }
}
