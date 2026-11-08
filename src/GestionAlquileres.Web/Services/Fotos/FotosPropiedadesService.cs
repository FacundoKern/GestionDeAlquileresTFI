using GestionAlquileres.Web.Data;
using GestionAlquileres.Web.Infrastructure.Configuracion;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Services.Comun;
using GestionAlquileres.Web.ViewModels.Propiedades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GestionAlquileres.Web.Services.Fotos;

/// <summary>
/// Fotos de las propiedades. Cada foto subida se valida y se procesa (<see cref="ProcesadorImagenes"/>), el archivo
/// se guarda en el <see cref="IAlmacenFotos"/> y el registro en la base (auditado como cualquier otro cambio).
/// Eliminar es borrado lógico: el archivo se conserva. La primera foto de una propiedad queda como principal.
/// Las acciones sobre una foto devuelven también su propiedad (para volver a la ficha), o null si la foto no existe.
/// </summary>
public interface IFotosPropiedadesService
{
    Task<GaleriaFotosViewModel> ObtenerGaleriaAsync(int propiedadId, string direccion, CancellationToken ct = default);

    Task<ResultadoSubidaFotos?> SubirAsync(int propiedadId, IReadOnlyList<IFormFile> archivos, CancellationToken ct = default);

    Task<ResultadoAccionFoto?> ActualizarDescripcionAsync(int fotoId, string? descripcion, string? rowVersion, CancellationToken ct = default);

    Task<ResultadoAccionFoto?> MarcarPrincipalAsync(int fotoId, CancellationToken ct = default);

    Task<EliminarFotoViewModel?> ObtenerParaEliminarAsync(int fotoId, CancellationToken ct = default);

    Task<ResultadoAccionFoto?> EliminarAsync(int fotoId, string? rowVersion, CancellationToken ct = default);

    /// <summary>Ruta física de la foto (o su miniatura) si existe y su propiedad no está eliminada.</summary>
    Task<string?> ObtenerRutaAsync(int fotoId, bool miniatura, CancellationToken ct = default);
}

public sealed record ResultadoAccionFoto(int PropiedadId, ResultadoOperacion Resultado);

public sealed class FotosPropiedadesService : IFotosPropiedadesService
{
    public const int LargoMaximoDescripcion = 200;

    private readonly AppDbContext _db;
    private readonly IAlmacenFotos _almacen;
    private readonly FotosOptions _opciones;

    public FotosPropiedadesService(AppDbContext db, IAlmacenFotos almacen, IOptions<FotosOptions> opciones)
    {
        _db = db;
        _almacen = almacen;
        _opciones = opciones.Value;
    }

    public async Task<GaleriaFotosViewModel> ObtenerGaleriaAsync(int propiedadId, string direccion, CancellationToken ct = default)
    {
        var fotos = await _db.FotosPropiedades.AsNoTracking()
            .Where(f => f.PropiedadId == propiedadId)
            .OrderByDescending(f => f.EsPrincipal).ThenBy(f => f.Orden)
            .Select(f => new { f.Id, f.Descripcion, f.EsPrincipal, f.Ancho, f.Alto, f.RowVersion })
            .ToListAsync(ct);

        return new GaleriaFotosViewModel(
            propiedadId,
            direccion,
            fotos.Select(f => new FotoPropiedadViewModel(f.Id, f.Descripcion, f.EsPrincipal, f.Ancho, f.Alto, Convert.ToBase64String(f.RowVersion))).ToList(),
            _opciones.MaximoPorPropiedad,
            _opciones.MaximoPorEnvio,
            _opciones.TamanioMaximoMB);
    }

    public async Task<ResultadoSubidaFotos?> SubirAsync(int propiedadId, IReadOnlyList<IFormFile> archivos, CancellationToken ct = default)
    {
        if (!await _db.Propiedades.AnyAsync(p => p.Id == propiedadId, ct))
        {
            return null;
        }

        if (archivos.Count == 0)
        {
            return new ResultadoSubidaFotos(0, ["Elegí al menos una foto."]);
        }

        if (archivos.Count > _opciones.MaximoPorEnvio)
        {
            return new ResultadoSubidaFotos(0, [$"Podés subir hasta {_opciones.MaximoPorEnvio} fotos por vez."]);
        }

        var existentes = await _db.FotosPropiedades.Where(f => f.PropiedadId == propiedadId)
            .Select(f => new { f.Orden, f.EsPrincipal })
            .ToListAsync(ct);
        var lugares = _opciones.MaximoPorPropiedad - existentes.Count;
        if (archivos.Count > lugares)
        {
            return new ResultadoSubidaFotos(0, [lugares <= 0
                ? $"La propiedad ya tiene el máximo de {_opciones.MaximoPorPropiedad} fotos. Eliminá alguna para subir otras."
                : $"Solo se pueden agregar {lugares} fotos más (máximo {_opciones.MaximoPorPropiedad} por propiedad)."]);
        }

        var errores = new List<string>();
        var nuevas = new List<FotoPropiedad>();
        var orden = existentes.Count == 0 ? 0 : existentes.Max(f => f.Orden);
        var hayPrincipal = existentes.Any(f => f.EsPrincipal);

        foreach (var archivo in archivos)
        {
            var nombre = NombreParaMostrar(archivo.FileName);
            if (archivo.Length > _opciones.TamanioMaximoBytes)
            {
                errores.Add($"«{nombre}» pesa más de {_opciones.TamanioMaximoMB} MB.");
                continue;
            }

            await using var contenido = archivo.OpenReadStream();
            var (imagen, error) = await ProcesadorImagenes.ProcesarAsync(contenido, _opciones, ct);
            if (imagen is null)
            {
                errores.Add($"«{nombre}» {error}");
                continue;
            }

            var foto = new FotoPropiedad
            {
                PropiedadId = propiedadId,
                Archivo = Guid.NewGuid().ToString("N"),
                Orden = ++orden,
                EsPrincipal = !hayPrincipal,
                Ancho = imagen.Ancho,
                Alto = imagen.Alto,
                TamanioBytes = imagen.Foto.LongLength,
                NombreOriginal = nombre,
            };
            hayPrincipal = true;

            await _almacen.GuardarAsync(foto.Archivo, imagen, ct);
            nuevas.Add(foto);
        }

        if (nuevas.Count > 0)
        {
            _db.FotosPropiedades.AddRange(nuevas);
            await _db.SaveChangesAsync(ct);
        }

        return new ResultadoSubidaFotos(nuevas.Count, errores);
    }

    public async Task<ResultadoAccionFoto?> ActualizarDescripcionAsync(int fotoId, string? descripcion, string? rowVersion, CancellationToken ct = default)
    {
        var foto = await BuscarAsync(fotoId, ct);
        if (foto is null)
        {
            return null;
        }

        var texto = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
        if (texto?.Length > LargoMaximoDescripcion)
        {
            return new(foto.PropiedadId, ResultadoOperacion.Fallo($"La descripción puede tener hasta {LargoMaximoDescripcion} caracteres."));
        }

        EstablecerVersionOriginal(foto, rowVersion);
        foto.Descripcion = texto;
        return new(foto.PropiedadId, await GuardarAsync(ct));
    }

    public async Task<ResultadoAccionFoto?> MarcarPrincipalAsync(int fotoId, CancellationToken ct = default)
    {
        var foto = await BuscarAsync(fotoId, ct);
        if (foto is null)
        {
            return null;
        }

        var anteriores = await _db.FotosPropiedades
            .Where(f => f.PropiedadId == foto.PropiedadId && f.EsPrincipal && f.Id != fotoId)
            .ToListAsync(ct);
        anteriores.ForEach(f => f.EsPrincipal = false);
        foto.EsPrincipal = true;

        return new(foto.PropiedadId, await GuardarAsync(ct));
    }

    public async Task<EliminarFotoViewModel?> ObtenerParaEliminarAsync(int fotoId, CancellationToken ct = default)
    {
        var foto = await _db.FotosPropiedades.AsNoTracking()
            .Where(f => f.Id == fotoId && !f.Propiedad.Eliminado)
            .Select(f => new
            {
                f.Id,
                f.PropiedadId,
                f.Propiedad.Calle,
                f.Propiedad.Numero,
                f.Propiedad.Piso,
                f.Propiedad.Departamento,
                f.Propiedad.Localidad,
                f.Descripcion,
                f.RowVersion,
            })
            .FirstOrDefaultAsync(ct);

        return foto is null
            ? null
            : new EliminarFotoViewModel(
                foto.Id,
                foto.PropiedadId,
                Propiedad.FormatearDireccion(foto.Calle, foto.Numero, foto.Piso, foto.Departamento, foto.Localidad),
                foto.Descripcion,
                Convert.ToBase64String(foto.RowVersion));
    }

    public async Task<ResultadoAccionFoto?> EliminarAsync(int fotoId, string? rowVersion, CancellationToken ct = default)
    {
        var foto = await BuscarAsync(fotoId, ct);
        if (foto is null)
        {
            return null;
        }

        EstablecerVersionOriginal(foto, rowVersion);
        _db.FotosPropiedades.Remove(foto); // borrado lógico y auditado (el archivo se conserva)

        if (foto.EsPrincipal)
        {
            // La siguiente foto pasa a ser la principal, para que la propiedad no quede sin imagen en el listado.
            var siguiente = await _db.FotosPropiedades
                .Where(f => f.PropiedadId == foto.PropiedadId && f.Id != fotoId)
                .OrderBy(f => f.Orden)
                .FirstOrDefaultAsync(ct);
            if (siguiente is not null)
            {
                siguiente.EsPrincipal = true;
            }
        }

        return new(foto.PropiedadId, await GuardarAsync(ct));
    }

    public async Task<string?> ObtenerRutaAsync(int fotoId, bool miniatura, CancellationToken ct = default)
    {
        var archivo = await _db.FotosPropiedades.AsNoTracking()
            .Where(f => f.Id == fotoId && !f.Propiedad.Eliminado)
            .Select(f => f.Archivo)
            .FirstOrDefaultAsync(ct);

        return archivo is null ? null : _almacen.RutaDe(archivo, miniatura);
    }

    /// <summary>El nombre del archivo lo eligió el usuario: se recorta y solo se usa como texto (Razor lo codifica).</summary>
    private static string NombreParaMostrar(string? nombre)
    {
        var limpio = Path.GetFileName(nombre ?? string.Empty).Trim();
        if (limpio.Length == 0)
        {
            return "Archivo sin nombre";
        }

        return limpio.Length > 100 ? limpio[..100] + "…" : limpio;
    }

    private void EstablecerVersionOriginal(FotoPropiedad foto, string? rowVersion)
    {
        if (!string.IsNullOrEmpty(rowVersion))
        {
            _db.Entry(foto).Property(f => f.RowVersion).OriginalValue = Convert.FromBase64String(rowVersion);
        }
    }

    /// <summary>Foto para modificar; null si no existe o su propiedad fue eliminada.</summary>
    private Task<FotoPropiedad?> BuscarAsync(int fotoId, CancellationToken ct) =>
        _db.FotosPropiedades.FirstOrDefaultAsync(f => f.Id == fotoId && !f.Propiedad.Eliminado, ct);

    private async Task<ResultadoOperacion> GuardarAsync(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
            return ResultadoOperacion.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ResultadoOperacion.ConflictoConcurrencia();
        }
    }
}
