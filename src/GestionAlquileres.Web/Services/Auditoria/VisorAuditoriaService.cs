using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using GestionAlquileres.Web.Data;
using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.ViewModels.Auditoria;
using GestionAlquileres.Web.ViewModels.Comun;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Web.Services.Auditoria;

/// <summary>Consulta del registro de auditoría (solo Administrador: contiene datos personales).</summary>
public interface IVisorAuditoriaService
{
    Task<AuditoriaIndexViewModel> ListarAsync(FiltroAuditoria filtro, CancellationToken ct = default);
}

public sealed partial class VisorAuditoriaService : IVisorAuditoriaService
{
    private const int TamanioPagina = 50;

    /// <summary>Nombre para mostrar de cada tipo de registro (el AuditLog guarda el nombre de la clase).</summary>
    private static readonly Dictionary<string, string> NombresEntidades = new()
    {
        [nameof(Propietario)] = "Propietario",
        [nameof(Propiedad)] = "Propiedad",
        [nameof(PropiedadPropietario)] = "Propietario de una propiedad",
        [nameof(FotoPropiedad)] = "Foto de propiedad",
        [nameof(Inquilino)] = "Inquilino",
        [nameof(Garante)] = "Garante",
        [nameof(Contrato)] = "Contrato",
        [nameof(ContratoInquilino)] = "Inquilino de un contrato",
        [nameof(ContratoGarante)] = "Garante de un contrato",
        [nameof(ContratoImpuesto)] = "Impuesto de un contrato",
        [nameof(ConfiguracionAjuste)] = "Configuración de ajuste",
        [nameof(HistorialMonto)] = "Cambio de monto",
        [nameof(Indice)] = "Índice",
        [nameof(IndiceValor)] = "Valor de índice",
        [nameof(Cuota)] = "Cuota",
        [nameof(Recibo)] = "Recibo",
        [nameof(ReciboItem)] = "Ítem de recibo",
        [nameof(Mora)] = "Mora",
        [nameof(ConfiguracionMora)] = "Configuración de mora",
        [nameof(Impuesto)] = "Impuesto",
        [nameof(ImpuestoVigencia)] = "Vigencia de impuesto",
        [nameof(DatosEmpresa)] = "Datos de la inmobiliaria",
        [nameof(Usuario)] = "Usuario",
    };

    /// <summary>Fichas que se pueden abrir desde el visor (controlador, acción, área).</summary>
    private static readonly Dictionary<string, (string Controlador, string Accion, string Area)> Fichas = new()
    {
        [nameof(Propietario)] = ("Propietarios", "Detalle", ""),
        [nameof(Propiedad)] = ("Propiedades", "Detalle", ""),
        [nameof(Inquilino)] = ("Inquilinos", "Detalle", ""),
        [nameof(Garante)] = ("Garantes", "Detalle", ""),
        [nameof(Usuario)] = ("Usuarios", "Editar", "Administracion"),
    };

    private readonly AppDbContext _db;

    public VisorAuditoriaService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AuditoriaIndexViewModel> ListarAsync(FiltroAuditoria filtro, CancellationToken ct = default)
    {
        var consulta = _db.AuditLogs.AsNoTracking();

        if (filtro.Desde is { } desde)
        {
            var desdeUtc = CulturaArgentina.InicioDelDiaEnUtc(desde);
            consulta = consulta.Where(a => a.FechaUtc >= desdeUtc);
        }

        if (filtro.Hasta is { } hasta)
        {
            var hastaUtc = CulturaArgentina.InicioDelDiaEnUtc(hasta.AddDays(1)); // incluye todo el día
            consulta = consulta.Where(a => a.FechaUtc < hastaUtc);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Usuario))
        {
            var usuario = filtro.Usuario.Trim();
            consulta = consulta.Where(a => a.UsuarioNombre!.Contains(usuario));
        }

        if (filtro.Accion is { } accion)
        {
            consulta = consulta.Where(a => a.Accion == accion);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Entidad))
        {
            consulta = consulta.Where(a => a.Entidad == filtro.Entidad);
        }

        if (!string.IsNullOrWhiteSpace(filtro.EntidadId))
        {
            var entidadId = filtro.EntidadId.Trim();
            consulta = consulta.Where(a => a.EntidadId == entidadId);
        }

        var pagina = await consulta
            .OrderByDescending(a => a.Id)
            .ToListaPaginadaAsync(filtro.Pagina, TamanioPagina, ct);

        // Pocos usuarios: el Id (GUID) se muestra como nombre.
        var nombresUsuarios = await _db.Users.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.NombreCompleto, ct);

        var entidades = (await _db.AuditLogs.AsNoTracking().Select(a => a.Entidad).Distinct().ToListAsync(ct))
            .Select(e => new OpcionEntidadViewModel(e, NombreEntidad(e)))
            .OrderBy(e => e.Nombre, StringComparer.Create(CulturaArgentina.Cultura, ignoreCase: true))
            .ToList();

        return new AuditoriaIndexViewModel(filtro, pagina.Mapear(a => Convertir(a, nombresUsuarios)), entidades);
    }

    private static RegistroAuditoriaViewModel Convertir(AuditLog a, IReadOnlyDictionary<string, string> nombresUsuarios)
    {
        var nombreEntidad = NombreEntidad(a.Entidad);
        var registro = a.Entidad == nameof(Usuario) && a.EntidadId is { } idUsuario && nombresUsuarios.TryGetValue(idUsuario, out var nombre)
            ? nombre
            : a.EntidadId is null ? nombreEntidad : $"{nombreEntidad} {a.EntidadId}";

        // Lo eliminado (borrado lógico) ya no tiene ficha visible.
        EnlaceFicha? enlace = null;
        if (a.EntidadId is { } id && a.Accion != Models.Enums.AccionAuditoria.Eliminar && Fichas.TryGetValue(a.Entidad, out var ficha))
        {
            enlace = new EnlaceFicha(ficha.Controlador, ficha.Accion, ficha.Area, id);
        }

        return new RegistroAuditoriaViewModel(
            a.Id,
            a.FechaUtc,
            a.UsuarioNombre ?? "—",
            a.Accion,
            a.Entidad,
            nombreEntidad,
            a.EntidadId,
            registro,
            enlace,
            a.Detalle,
            a.DireccionIp,
            Cambios(a.ValoresAnteriores, a.ValoresNuevos));
    }

    private static string NombreEntidad(string entidad) => NombresEntidades.GetValueOrDefault(entidad, entidad);

    /// <summary>
    /// Une los JSON de antes y después en una lista campo por campo. En altas y bajas (un solo lado) se omiten
    /// los campos vacíos y los de borrado lógico, que no aportan.
    /// </summary>
    private static IReadOnlyList<CambioCampoViewModel> Cambios(string? antesJson, string? despuesJson)
    {
        var antes = Leer(antesJson);
        var despues = Leer(despuesJson);
        var ambosLados = antes.Count > 0 && despues.Count > 0;

        return antes.Keys.Union(despues.Keys)
            .Where(campo => campo != "Id")
            .Select(campo => (
                Campo: campo,
                Antes: antes.TryGetValue(campo, out var a) ? a : null,
                Despues: despues.TryGetValue(campo, out var d) ? d : null))
            .Where(c => ambosLados || (!CamposBorradoLogico.Contains(c.Campo) && (c.Antes ?? c.Despues) != SinValor))
            .Select(c => new CambioCampoViewModel(NombreCampo(c.Campo), c.Antes, c.Despues))
            .ToList();
    }

    private const string SinValor = "—";

    private static readonly HashSet<string> CamposBorradoLogico = ["Eliminado", "EliminadoEnUtc", "EliminadoPor"];

    /// <summary>Palabras de los nombres de campo que llevan tilde (el código no las usa).</summary>
    private static readonly Dictionary<string, string> Tildes = new()
    {
        ["numero"] = "número",
        ["telefono"] = "teléfono",
        ["razon"] = "razón",
        ["descripcion"] = "descripción",
        ["direccion"] = "dirección",
        ["codigo"] = "código",
        ["matricula"] = "matrícula",
        ["ultimo"] = "último",
        ["rescision"] = "rescisión",
        ["periodo"] = "período",
        ["indice"] = "índice",
        ["emision"] = "emisión",
        ["anulacion"] = "anulación",
        ["dia"] = "día",
        ["dias"] = "días",
        ["utc"] = "(UTC)",
    };

    private static Dictionary<string, string> Leer(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using var documento = JsonDocument.Parse(json);
            return documento.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => Valor(p.Value));
        }
        catch (JsonException)
        {
            return new Dictionary<string, string> { ["(datos)"] = json };
        }
    }

    private static string Valor(JsonElement valor) => valor.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => SinValor,
        JsonValueKind.True => "Sí",
        JsonValueKind.False => "No",
        JsonValueKind.Number => valor.TryGetDecimal(out var numero) ? numero.ToString(CulturaArgentina.Cultura) : valor.GetRawText(),
        JsonValueKind.String => FormatearTexto(valor.GetString()!),
        _ => valor.GetRawText(),
    };

    /// <summary>Fechas ISO a formato argentino; el resto, tal cual.</summary>
    private static string FormatearTexto(string texto)
    {
        if (texto.Length == 10 && DateOnly.TryParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
        {
            return Formato.Fecha(fecha);
        }

        if (texto.Length >= 19 && texto[10] == 'T'
            && DateTime.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var instante))
        {
            return Formato.FechaHora(instante);
        }

        return texto;
    }

    /// <summary>"NumeroDocumento" → "Número documento".</summary>
    private static string NombreCampo(string campo)
    {
        var palabras = MayusculaInterna().Replace(campo, " $1").ToLowerInvariant()
            .Split(' ')
            .Select(p => Tildes.GetValueOrDefault(p, p));
        var texto = string.Join(' ', palabras);
        return char.ToUpper(texto[0], CulturaArgentina.Cultura) + texto[1..];
    }

    [GeneratedRegex("(?<=[a-z])([A-Z])")]
    private static partial Regex MayusculaInterna();
}
