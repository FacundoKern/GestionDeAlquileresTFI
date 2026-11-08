using GestionAlquileres.Web.Data;
using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Services.Comun;
using GestionAlquileres.Web.ViewModels.Empresa;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GestionAlquileres.Web.Services.Empresa;

public interface IDatosEmpresaService
{
    /// <summary>Datos para mostrar. Null si todavía no se cargaron. Se cachean en memoria.</summary>
    Task<DatosEmpresaViewModel?> ObtenerAsync(CancellationToken ct = default);

    Task<DatosEmpresaFormViewModel> ObtenerParaEditarAsync(CancellationToken ct = default);

    Task<ResultadoOperacion> GuardarAsync(DatosEmpresaFormViewModel modelo, CancellationToken ct = default);
}

public sealed class DatosEmpresaService : IDatosEmpresaService
{
    // Se leen en cada página (pie de página): se cachean y se invalidan al guardar.
    // Nota: la caché es por instancia; si la app se escala a varios servidores, usar caché distribuida.
    private const string ClaveCache = "DatosEmpresa";
    private static readonly TimeSpan DuracionCache = TimeSpan.FromMinutes(30);

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public DatosEmpresaService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<DatosEmpresaViewModel?> ObtenerAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(ClaveCache, out DatosEmpresaViewModel? enCache))
        {
            return enCache;
        }

        var datos = await _db.DatosEmpresa.AsNoTracking()
            .OrderBy(d => d.Id)
            .Select(d => new DatosEmpresaViewModel
            {
                NombreComercial = d.NombreComercial,
                RazonSocial = d.RazonSocial,
                Cuit = d.Cuit,
                MatriculaCorredor = d.MatriculaCorredor,
                Direccion = d.Direccion,
                Localidad = d.Localidad,
                Provincia = d.Provincia,
                CodigoPostal = d.CodigoPostal,
                Telefono = d.Telefono,
                Whatsapp = d.Whatsapp,
                Email = d.Email,
                SitioWeb = d.SitioWeb,
                HorarioAtencion = d.HorarioAtencion,
                Descripcion = d.Descripcion,
                SoporteNombre = d.SoporteNombre,
                SoporteTelefono = d.SoporteTelefono,
                SoporteEmail = d.SoporteEmail,
            })
            .FirstOrDefaultAsync(ct);

        _cache.Set(ClaveCache, datos, DuracionCache);
        return datos;
    }

    public async Task<DatosEmpresaFormViewModel> ObtenerParaEditarAsync(CancellationToken ct = default)
    {
        var d = await _db.DatosEmpresa.AsNoTracking().OrderBy(d => d.Id).FirstOrDefaultAsync(ct);
        if (d is null)
        {
            return new DatosEmpresaFormViewModel();
        }

        return new DatosEmpresaFormViewModel
        {
            NombreComercial = d.NombreComercial,
            RazonSocial = d.RazonSocial,
            Cuit = d.Cuit is null ? null : Documentos.Formatear(Models.Enums.TipoDocumento.Cuit, d.Cuit),
            MatriculaCorredor = d.MatriculaCorredor,
            Direccion = d.Direccion,
            Localidad = d.Localidad,
            Provincia = d.Provincia,
            CodigoPostal = d.CodigoPostal,
            Telefono = d.Telefono,
            Whatsapp = d.Whatsapp,
            Email = d.Email,
            SitioWeb = d.SitioWeb,
            HorarioAtencion = d.HorarioAtencion,
            Descripcion = d.Descripcion,
            SoporteNombre = d.SoporteNombre,
            SoporteTelefono = d.SoporteTelefono,
            SoporteEmail = d.SoporteEmail,
            RowVersion = Convert.ToBase64String(d.RowVersion),
        };
    }

    public async Task<ResultadoOperacion> GuardarAsync(DatosEmpresaFormViewModel modelo, CancellationToken ct = default)
    {
        var entidad = await _db.DatosEmpresa.OrderBy(d => d.Id).FirstOrDefaultAsync(ct);
        if (entidad is null)
        {
            entidad = new DatosEmpresa();
            _db.DatosEmpresa.Add(entidad);
        }
        else if (!string.IsNullOrEmpty(modelo.RowVersion))
        {
            // Si otro usuario guardó después de que se abrió el formulario, el UPDATE no encuentra la fila.
            _db.Entry(entidad).Property(e => e.RowVersion).OriginalValue = Convert.FromBase64String(modelo.RowVersion);
        }

        entidad.NombreComercial = modelo.NombreComercial.Trim();
        entidad.RazonSocial = Limpiar(modelo.RazonSocial);
        entidad.Cuit = string.IsNullOrWhiteSpace(modelo.Cuit) ? null : Documentos.Normalizar(modelo.Cuit);
        entidad.MatriculaCorredor = Limpiar(modelo.MatriculaCorredor);
        entidad.Direccion = Limpiar(modelo.Direccion);
        entidad.Localidad = Limpiar(modelo.Localidad);
        entidad.Provincia = Limpiar(modelo.Provincia);
        entidad.CodigoPostal = Limpiar(modelo.CodigoPostal);
        entidad.Telefono = Limpiar(modelo.Telefono);
        entidad.Whatsapp = Limpiar(modelo.Whatsapp);
        entidad.Email = Limpiar(modelo.Email);
        entidad.SitioWeb = Limpiar(modelo.SitioWeb);
        entidad.HorarioAtencion = Limpiar(modelo.HorarioAtencion);
        entidad.Descripcion = Limpiar(modelo.Descripcion);
        entidad.SoporteNombre = Limpiar(modelo.SoporteNombre);
        entidad.SoporteTelefono = Limpiar(modelo.SoporteTelefono);
        entidad.SoporteEmail = Limpiar(modelo.SoporteEmail);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ResultadoOperacion.ConflictoConcurrencia();
        }

        _cache.Remove(ClaveCache);
        return ResultadoOperacion.Ok();
    }

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
