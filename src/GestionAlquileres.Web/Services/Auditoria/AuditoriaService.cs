using GestionAlquileres.Web.Data;
using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Models.Enums;
using GestionAlquileres.Web.Security;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Web.Services.Auditoria;

/// <summary>
/// Registro explícito de eventos que no son cambios de datos (los cambios de datos se auditan solos
/// en el DbContext): inicios de sesión, consultas de fichas con datos personales, gestión de usuarios.
/// </summary>
public interface IAuditoriaService
{
    Task RegistrarEventoAsync(AccionAuditoria accion, string entidad, string? entidadId = null, string? detalle = null, CancellationToken ct = default);

    Task RegistrarConsultaDatosPersonalesAsync(string entidad, int id, CancellationToken ct = default);
}

public sealed class AuditoriaService : IAuditoriaService
{
    private readonly DbContextOptions<AppDbContext> _opcionesDb;
    private readonly IUsuarioActual _usuario;
    private readonly TimeProvider _reloj;

    public AuditoriaService(DbContextOptions<AppDbContext> opcionesDb, IUsuarioActual usuario, TimeProvider reloj)
    {
        _opcionesDb = opcionesDb;
        _usuario = usuario;
        _reloj = reloj;
    }

    public async Task RegistrarEventoAsync(
        AccionAuditoria accion,
        string entidad,
        string? entidadId = null,
        string? detalle = null,
        CancellationToken ct = default)
    {
        // Contexto propio y de vida corta: el evento se guarda solo, sin arrastrar (ni depender de)
        // cambios pendientes del contexto del request. Por ejemplo, un usuario que Identity no pudo
        // actualizar por concurrencia no debe hacer fallar el registro del evento.
        await using var db = new AppDbContext(_opcionesDb, _usuario, _reloj);
        db.AuditLogs.Add(new AuditLog
        {
            FechaUtc = _reloj.AhoraUtc(),
            UsuarioId = _usuario.Id,
            UsuarioNombre = _usuario.Nombre,
            Accion = accion,
            Entidad = entidad,
            EntidadId = entidadId,
            Detalle = detalle is { Length: > 1000 } ? detalle[..1000] : detalle,
            DireccionIp = _usuario.DireccionIp,
        });
        await db.SaveChangesAsync(ct);
    }

    public Task RegistrarConsultaDatosPersonalesAsync(string entidad, int id, CancellationToken ct = default) =>
        RegistrarEventoAsync(AccionAuditoria.ConsultaDatosPersonales, entidad, id.ToString(), "Apertura de ficha", ct);
}
