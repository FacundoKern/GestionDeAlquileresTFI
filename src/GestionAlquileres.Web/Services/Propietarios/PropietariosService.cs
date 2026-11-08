using GestionAlquileres.Web.Data;
using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Models.Enums;
using GestionAlquileres.Web.Services.Comun;
using GestionAlquileres.Web.Services.Contratos;
using GestionAlquileres.Web.Services.Personas;
using GestionAlquileres.Web.ViewModels.Comun;
using GestionAlquileres.Web.ViewModels.Personas;
using GestionAlquileres.Web.ViewModels.Propietarios;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Web.Services.Propietarios;

public interface IPropietariosService
{
    Task<ListaPaginada<PropietarioFilaViewModel>> ListarAsync(FiltroPropietarios filtro, AlcanceDatos alcance, CancellationToken ct = default);

    Task<PropietarioDetalleViewModel?> ObtenerDetalleAsync(int id, AlcanceDatos alcance, CancellationToken ct = default);

    Task<PropietarioFormViewModel?> ObtenerParaEditarAsync(int id, CancellationToken ct = default);

    Task<ResultadoOperacion> CrearAsync(PropietarioFormViewModel modelo, CancellationToken ct = default);

    Task<ResultadoOperacion> ActualizarAsync(int id, PropietarioFormViewModel modelo, CancellationToken ct = default);

    Task<EliminarPersonaViewModel?> ObtenerParaEliminarAsync(int id, CancellationToken ct = default);

    Task<ResultadoOperacion> EliminarAsync(int id, string? rowVersion, CancellationToken ct = default);
}

public sealed class PropietariosService : IPropietariosService
{
    private const string MensajeDocumentoDuplicado = "Ya existe un propietario con ese tipo y número de documento.";

    private readonly AppDbContext _db;
    private readonly IResumenContratosService _contratos;

    public PropietariosService(AppDbContext db, IResumenContratosService contratos)
    {
        _db = db;
        _contratos = contratos;
    }

    public async Task<ListaPaginada<PropietarioFilaViewModel>> ListarAsync(
        FiltroPropietarios filtro,
        AlcanceDatos alcance,
        CancellationToken ct = default)
    {
        var consulta = _db.Propietarios.AsNoTracking();

        foreach (var palabra in BusquedaTexto.Palabras(filtro.Texto))
        {
            var digitos = Documentos.Normalizar(palabra);
            consulta = consulta.Where(p =>
                EF.Functions.Collate(p.Nombre, BusquedaTexto.SinAcentos).Contains(palabra)
                || EF.Functions.Collate(p.Apellido!, BusquedaTexto.SinAcentos).Contains(palabra)
                || (digitos.Length >= 3 && p.NumeroDocumento.Contains(digitos)));
        }

        foreach (var palabra in BusquedaTexto.Palabras(filtro.Propiedad))
        {
            consulta = consulta.Where(p => p.Propiedades.Any(pp =>
                EF.Functions.Collate(pp.Propiedad.Calle, BusquedaTexto.SinAcentos).Contains(palabra)
                || pp.Propiedad.Numero.Contains(palabra)
                || EF.Functions.Collate(pp.Propiedad.Localidad, BusquedaTexto.SinAcentos).Contains(palabra)));
        }

        var pagina = await consulta
            .OrderBy(p => p.Apellido ?? p.Nombre)
            .ThenBy(p => p.Nombre)
            .Select(p => new
            {
                p.Id,
                p.TipoPersona,
                p.Nombre,
                p.Apellido,
                p.TipoDocumento,
                p.NumeroDocumento,
                p.Telefono,
                p.Email,
                CantidadPropiedades = p.Propiedades.Count,
            })
            .ToListaPaginadaAsync(filtro.Pagina, ct: ct);

        return pagina.Mapear(p => new PropietarioFilaViewModel(
            p.Id,
            Models.Base.Persona.FormatearNombre(p.TipoPersona, p.Nombre, p.Apellido),
            PersonaMapeo.MostrarDocumento(p.TipoDocumento, p.NumeroDocumento, alcance.DatosPersonalesCompletos),
            PersonaMapeo.MostrarTelefono(p.Telefono, alcance.DatosPersonalesCompletos),
            PersonaMapeo.MostrarEmail(p.Email, alcance.DatosPersonalesCompletos),
            p.CantidadPropiedades));
    }

    public async Task<PropietarioDetalleViewModel?> ObtenerDetalleAsync(int id, AlcanceDatos alcance, CancellationToken ct = default)
    {
        var propietario = await _db.Propietarios.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (propietario is null)
        {
            return null;
        }

        var propiedades = await _db.PropiedadesPropietarios.AsNoTracking()
            .Where(pp => pp.PropietarioId == id)
            .Select(pp => new
            {
                pp.PropiedadId,
                pp.Propiedad.Calle,
                pp.Propiedad.Numero,
                pp.Propiedad.Piso,
                pp.Propiedad.Departamento,
                pp.Propiedad.Localidad,
                pp.Propiedad.Tipo,
                pp.Propiedad.Activa,
                pp.Porcentaje,
                TieneContratoVigente = pp.Propiedad.Contratos.Any(c => c.Estado == EstadoContrato.Vigente),
            })
            .OrderBy(pp => pp.Calle).ThenBy(pp => pp.Numero)
            .ToListAsync(ct);

        // Sin permiso, los contratos ni siquiera se consultan (null = "no disponible para tu rol").
        var contratos = alcance.Contratos
            ? await _contratos.ObtenerAsync(c => c.Propiedad.Propietarios.Any(pp => pp.PropietarioId == id), ct)
            : null;

        return new PropietarioDetalleViewModel(
            propietario.Id,
            PersonaMapeo.ADatos(propietario, alcance.DatosPersonalesCompletos),
            propiedades.Select(pp => new PropiedadDePropietarioViewModel(
                pp.PropiedadId,
                Propiedad.FormatearDireccion(pp.Calle, pp.Numero, pp.Piso, pp.Departamento, pp.Localidad),
                pp.Tipo,
                pp.Porcentaje,
                Propiedad.CalcularEstado(pp.Activa, pp.TieneContratoVigente))).ToList(),
            contratos);
    }

    public async Task<PropietarioFormViewModel?> ObtenerParaEditarAsync(int id, CancellationToken ct = default)
    {
        var propietario = await _db.Propietarios.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return propietario is null ? null : PersonaMapeo.AFormulario<PropietarioFormViewModel>(propietario);
    }

    public async Task<ResultadoOperacion> CrearAsync(PropietarioFormViewModel modelo, CancellationToken ct = default)
    {
        if (await ExisteDocumentoAsync(modelo, excluirId: null, ct))
        {
            return ResultadoOperacion.Fallo(MensajeDocumentoDuplicado, nameof(modelo.NumeroDocumento));
        }

        var propietario = new Propietario();
        PersonaMapeo.CopiarA(modelo, propietario);
        _db.Propietarios.Add(propietario);

        return await GuardarAsync(ct) ?? ResultadoOperacion.Ok(propietario.Id);
    }

    public async Task<ResultadoOperacion> ActualizarAsync(int id, PropietarioFormViewModel modelo, CancellationToken ct = default)
    {
        var propietario = await _db.Propietarios.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (propietario is null)
        {
            return ResultadoOperacion.NoEncontrado();
        }

        if (await ExisteDocumentoAsync(modelo, excluirId: id, ct))
        {
            return ResultadoOperacion.Fallo(MensajeDocumentoDuplicado, nameof(modelo.NumeroDocumento));
        }

        EstablecerVersionOriginal(propietario, modelo.RowVersion);
        PersonaMapeo.CopiarA(modelo, propietario);

        return await GuardarAsync(ct) ?? ResultadoOperacion.Ok(id);
    }

    public async Task<EliminarPersonaViewModel?> ObtenerParaEliminarAsync(int id, CancellationToken ct = default)
    {
        var propietario = await _db.Propietarios.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (propietario is null)
        {
            return null;
        }

        return new EliminarPersonaViewModel(
            propietario.Id,
            propietario.NombreCompleto,
            PersonaMapeo.MostrarDocumento(propietario.TipoDocumento, propietario.NumeroDocumento, completo: true),
            Convert.ToBase64String(propietario.RowVersion),
            await MotivoQueImpideEliminarAsync(id, ct));
    }

    public async Task<ResultadoOperacion> EliminarAsync(int id, string? rowVersion, CancellationToken ct = default)
    {
        var propietario = await _db.Propietarios.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (propietario is null)
        {
            return ResultadoOperacion.NoEncontrado();
        }

        if (await MotivoQueImpideEliminarAsync(id, ct) is { } motivo)
        {
            return ResultadoOperacion.Fallo(motivo);
        }

        EstablecerVersionOriginal(propietario, rowVersion);
        _db.Propietarios.Remove(propietario); // el DbContext lo convierte en borrado lógico y lo audita

        return await GuardarAsync(ct) ?? ResultadoOperacion.Ok(id);
    }

    /// <summary>
    /// Regla: no se elimina a un propietario que figura en propiedades (se perdería el historial).
    /// Primero hay que quitarlo de esas propiedades.
    /// </summary>
    private async Task<string?> MotivoQueImpideEliminarAsync(int id, CancellationToken ct)
    {
        var propiedades = await _db.PropiedadesPropietarios.CountAsync(pp => pp.PropietarioId == id, ct);
        return propiedades == 0
            ? null
            : $"No se puede eliminar porque figura como propietario de {propiedades} {(propiedades == 1 ? "propiedad" : "propiedades")}. " +
              "Primero quitalo de esas propiedades.";
    }

    private Task<bool> ExisteDocumentoAsync(PersonaFormViewModel modelo, int? excluirId, CancellationToken ct)
    {
        var numero = Documentos.Normalizar(modelo.NumeroDocumento);
        return _db.Propietarios.AnyAsync(
            p => p.TipoDocumento == modelo.TipoDocumento && p.NumeroDocumento == numero && p.Id != excluirId, ct);
    }

    private void EstablecerVersionOriginal(Propietario propietario, string? rowVersion)
    {
        if (!string.IsNullOrEmpty(rowVersion))
        {
            _db.Entry(propietario).Property(p => p.RowVersion).OriginalValue = Convert.FromBase64String(rowVersion);
        }
    }

    /// <summary>Guarda y traduce los errores esperables. Devuelve null si salió bien.</summary>
    private async Task<ResultadoOperacion?> GuardarAsync(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            return ResultadoOperacion.ConflictoConcurrencia();
        }
        catch (DbUpdateException ex) when (ErroresBaseDeDatos.EsViolacionDeUnicidad(ex))
        {
            return ResultadoOperacion.Fallo(MensajeDocumentoDuplicado, nameof(PersonaFormViewModel.NumeroDocumento));
        }
    }
}
