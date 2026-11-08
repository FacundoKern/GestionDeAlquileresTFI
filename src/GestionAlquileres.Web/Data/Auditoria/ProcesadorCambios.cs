using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using GestionAlquileres.Web.Models.Base;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Models.Enums;
using GestionAlquileres.Web.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace GestionAlquileres.Web.Data.Auditoria;

/// <summary>
/// Se ejecuta antes de cada SaveChanges:
/// 1. Hace cumplir las reglas de integridad (registros inmutables, no eliminables, recibos emitidos).
/// 2. Convierte los borrados de entidades <see cref="IBorradoLogico"/> en borrados lógicos.
/// 3. Completa los campos de auditoría (creado/modificado por y cuándo).
/// 4. Prepara las entradas de AuditLog, que se completan y guardan después (ya con los Id generados).
/// </summary>
public sealed class ProcesadorCambios
{
    private static readonly HashSet<string> PropiedadesDeControl =
    [
        nameof(EntidadAuditable.CreadoEnUtc),
        nameof(EntidadAuditable.CreadoPor),
        nameof(EntidadAuditable.ModificadoEnUtc),
        nameof(EntidadAuditable.ModificadoPor),
        nameof(EntidadAuditable.RowVersion),
    ];

    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        Converters = { new JsonStringEnumConverter() },
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // conserva acentos legibles; se muestra siempre codificado por Razor
    };

    private readonly IUsuarioActual _usuario;
    private readonly DateTime _ahoraUtc;

    public ProcesadorCambios(IUsuarioActual usuario, DateTime ahoraUtc)
    {
        _usuario = usuario;
        _ahoraUtc = ahoraUtc;
    }

    public IReadOnlyList<CambioPendiente> Procesar(ChangeTracker changeTracker)
    {
        changeTracker.DetectChanges();
        var pendientes = new List<CambioPendiente>();

        foreach (var entry in changeTracker.Entries().ToList())
        {
            if (entry.State is EntityState.Detached or EntityState.Unchanged)
            {
                continue;
            }

            ValidarIntegridad(entry);

            var accion = entry.State switch
            {
                EntityState.Added => AccionAuditoria.Crear,
                EntityState.Deleted => AccionAuditoria.Eliminar,
                _ => EsRestauracion(entry) ? AccionAuditoria.Restaurar : AccionAuditoria.Modificar,
            };

            // Los valores "antes" se toman ahora: después de guardar, EF los descarta.
            Dictionary<string, object?>? antes = null;
            Dictionary<string, object?>? despues = null;
            if (entry.State == EntityState.Deleted)
            {
                antes = ValoresActuales(entry);
            }

            if (entry.State == EntityState.Deleted && entry.Entity is IBorradoLogico)
            {
                AplicarBorradoLogico(entry);
            }

            CompletarCamposDeAuditoria(entry);

            if (entry.State == EntityState.Modified && accion != AccionAuditoria.Eliminar)
            {
                (antes, despues) = ValoresModificados(entry);
                if (antes.Count == 0)
                {
                    continue; // solo cambiaron campos de control: nada que auditar
                }
            }

            if (EsAuditable(entry))
            {
                // La clave de una fila nueva se conoce recién después de guardar (identity).
                var clave = entry.State == EntityState.Added ? null : ObtenerClave(entry);
                pendientes.Add(new CambioPendiente(entry, accion, clave, antes, despues));
            }
        }

        return pendientes;
    }

    public AuditLog CrearRegistro(CambioPendiente cambio)
    {
        var entry = cambio.Entry;
        var despues = cambio.Accion == AccionAuditoria.Crear ? ValoresActuales(entry) : cambio.ValoresDespues;

        return new AuditLog
        {
            FechaUtc = _ahoraUtc,
            UsuarioId = _usuario.Id,
            UsuarioNombre = _usuario.Nombre,
            Accion = cambio.Accion,
            Entidad = entry.Metadata.ClrType.Name,
            EntidadId = cambio.Clave ?? ObtenerClave(entry),
            ValoresAnteriores = Serializar(cambio.ValoresAntes),
            ValoresNuevos = Serializar(despues),
            DireccionIp = _usuario.DireccionIp,
        };
    }

    public static string? Serializar(IReadOnlyDictionary<string, object?>? valores) =>
        valores is null || valores.Count == 0 ? null : JsonSerializer.Serialize(valores, OpcionesJson);

    private static void ValidarIntegridad(EntityEntry entry)
    {
        var nombre = entry.Metadata.ClrType.Name;

        if (entry.Entity is ISoloAgregar && entry.State is EntityState.Modified or EntityState.Deleted)
        {
            throw new InvalidOperationException($"{nombre} es un registro inmutable: solo se pueden agregar filas.");
        }

        if (entry.Entity is INoEliminable && entry.State == EntityState.Deleted)
        {
            throw new InvalidOperationException($"{nombre} no se puede eliminar.");
        }

        if (entry.Entity is IInmutableTrasEmision && entry.State == EntityState.Modified)
        {
            var noPermitidas = entry.Properties
                .Where(p => p.IsModified
                    && !PropiedadesDeControl.Contains(p.Metadata.Name)
                    && p.Metadata.PropertyInfo?.GetCustomAttribute<ModificableTrasEmisionAttribute>() is null)
                .Select(p => p.Metadata.Name)
                .ToList();

            if (noPermitidas.Count > 0)
            {
                throw new InvalidOperationException(
                    $"{nombre} ya fue emitido y no se puede modificar ({string.Join(", ", noPermitidas)}). Debe anularse.");
            }
        }
    }

    private void AplicarBorradoLogico(EntityEntry entry)
    {
        entry.State = EntityState.Unchanged;
        entry.Property(nameof(IBorradoLogico.Eliminado)).CurrentValue = true;
        entry.Property(nameof(IBorradoLogico.EliminadoEnUtc)).CurrentValue = _ahoraUtc;
        entry.Property(nameof(IBorradoLogico.EliminadoPor)).CurrentValue = _usuario.Nombre;
    }

    private static bool EsRestauracion(EntityEntry entry) =>
        entry.Entity is IBorradoLogico
        && entry.Property(nameof(IBorradoLogico.Eliminado)) is { IsModified: true, OriginalValue: true, CurrentValue: false };

    private void CompletarCamposDeAuditoria(EntityEntry entry)
    {
        if (entry.Entity is not EntidadAuditable)
        {
            return;
        }

        if (entry.State == EntityState.Added)
        {
            entry.Property(nameof(EntidadAuditable.CreadoEnUtc)).CurrentValue = _ahoraUtc;
            entry.Property(nameof(EntidadAuditable.CreadoPor)).CurrentValue = _usuario.Nombre;
        }
        else if (entry.State == EntityState.Modified)
        {
            // Los datos de creación nunca se pisan desde un formulario.
            entry.Property(nameof(EntidadAuditable.CreadoEnUtc)).IsModified = false;
            entry.Property(nameof(EntidadAuditable.CreadoPor)).IsModified = false;
            entry.Property(nameof(EntidadAuditable.ModificadoEnUtc)).CurrentValue = _ahoraUtc;
            entry.Property(nameof(EntidadAuditable.ModificadoPor)).CurrentValue = _usuario.Nombre;
        }
    }

    private static bool EsAuditable(EntityEntry entry)
    {
        var tipo = entry.Metadata.ClrType;
        return tipo.GetCustomAttribute<NoAuditarAttribute>() is null
            && tipo.Namespace?.StartsWith("GestionAlquileres", StringComparison.Ordinal) == true;
    }

    private static bool EsPropiedadAuditable(PropertyEntry propiedad) =>
        !PropiedadesDeControl.Contains(propiedad.Metadata.Name)
        && propiedad.Metadata.PropertyInfo?.GetCustomAttribute<NoAuditarAttribute>() is null;

    private static Dictionary<string, object?> ValoresActuales(EntityEntry entry) =>
        entry.Properties
            .Where(EsPropiedadAuditable)
            .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);

    private static (Dictionary<string, object?> Antes, Dictionary<string, object?> Despues) ValoresModificados(EntityEntry entry)
    {
        var antes = new Dictionary<string, object?>();
        var despues = new Dictionary<string, object?>();
        foreach (var propiedad in entry.Properties.Where(p => p.IsModified && EsPropiedadAuditable(p)))
        {
            if (Equals(propiedad.OriginalValue, propiedad.CurrentValue))
            {
                continue;
            }

            antes[propiedad.Metadata.Name] = propiedad.OriginalValue;
            despues[propiedad.Metadata.Name] = propiedad.CurrentValue;
        }

        return (antes, despues);
    }

    private static string? ObtenerClave(EntityEntry entry)
    {
        var clave = entry.Metadata.FindPrimaryKey();
        return clave is null
            ? null
            : string.Join(",", clave.Properties.Select(p => entry.Property(p.Name).CurrentValue));
    }
}

public sealed record CambioPendiente(
    EntityEntry Entry,
    AccionAuditoria Accion,
    string? Clave,
    IReadOnlyDictionary<string, object?>? ValoresAntes,
    IReadOnlyDictionary<string, object?>? ValoresDespues);
