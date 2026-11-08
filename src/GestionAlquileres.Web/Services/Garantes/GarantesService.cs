using GestionAlquileres.Web.Data;
using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Models.Enums;
using GestionAlquileres.Web.Services.Comun;
using GestionAlquileres.Web.Services.Contratos;
using GestionAlquileres.Web.Services.Personas;
using GestionAlquileres.Web.ViewModels.Comun;
using GestionAlquileres.Web.ViewModels.Garantes;
using GestionAlquileres.Web.ViewModels.Personas;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Web.Services.Garantes;

public interface IGarantesService
{
    Task<ListaPaginada<GaranteFilaViewModel>> ListarAsync(FiltroGarantes filtro, AlcanceDatos alcance, CancellationToken ct = default);

    Task<GaranteDetalleViewModel?> ObtenerDetalleAsync(int id, AlcanceDatos alcance, CancellationToken ct = default);

    Task<GaranteFormViewModel?> ObtenerParaEditarAsync(int id, CancellationToken ct = default);

    Task<ResultadoOperacion> CrearAsync(GaranteFormViewModel modelo, CancellationToken ct = default);

    Task<ResultadoOperacion> ActualizarAsync(int id, GaranteFormViewModel modelo, CancellationToken ct = default);

    Task<EliminarPersonaViewModel?> ObtenerParaEliminarAsync(int id, CancellationToken ct = default);

    Task<ResultadoOperacion> EliminarAsync(int id, string? rowVersion, CancellationToken ct = default);
}

public sealed class GarantesService : IGarantesService
{
    private const string MensajeDocumentoDuplicado = "Ya existe un garante con ese tipo y número de documento.";

    private readonly AppDbContext _db;
    private readonly IResumenContratosService _contratos;

    public GarantesService(AppDbContext db, IResumenContratosService contratos)
    {
        _db = db;
        _contratos = contratos;
    }

    public async Task<ListaPaginada<GaranteFilaViewModel>> ListarAsync(
        FiltroGarantes filtro,
        AlcanceDatos alcance,
        CancellationToken ct = default)
    {
        var consulta = _db.Garantes.AsNoTracking();

        foreach (var palabra in BusquedaTexto.Palabras(filtro.Texto))
        {
            var digitos = Documentos.Normalizar(palabra);
            consulta = consulta.Where(g =>
                EF.Functions.Collate(g.Nombre, BusquedaTexto.SinAcentos).Contains(palabra)
                || EF.Functions.Collate(g.Apellido!, BusquedaTexto.SinAcentos).Contains(palabra)
                || (digitos.Length >= 3 && g.NumeroDocumento.Contains(digitos)));
        }

        // El filtro por contrato solo se aplica a los roles que pueden ver contratos.
        if (alcance.Contratos && filtro.GarantizaContratoVigente is { } garantizaVigente)
        {
            consulta = consulta.Where(g => g.Contratos.Any(cg => cg.Contrato.Estado == EstadoContrato.Vigente) == garantizaVigente);
        }

        var pagina = await consulta
            .OrderBy(g => g.Apellido ?? g.Nombre)
            .ThenBy(g => g.Nombre)
            .Select(g => new
            {
                g.Id,
                g.TipoPersona,
                g.Nombre,
                g.Apellido,
                g.TipoDocumento,
                g.NumeroDocumento,
                g.Telefono,
                g.Email,
                Vigentes = g.Contratos.Count(cg => cg.Contrato.Estado == EstadoContrato.Vigente),
                Totales = g.Contratos.Count,
            })
            .ToListaPaginadaAsync(filtro.Pagina, ct: ct);

        return pagina.Mapear(g => new GaranteFilaViewModel(
            g.Id,
            Models.Base.Persona.FormatearNombre(g.TipoPersona, g.Nombre, g.Apellido),
            PersonaMapeo.MostrarDocumento(g.TipoDocumento, g.NumeroDocumento, alcance.DatosPersonalesCompletos),
            PersonaMapeo.MostrarTelefono(g.Telefono, alcance.DatosPersonalesCompletos),
            PersonaMapeo.MostrarEmail(g.Email, alcance.DatosPersonalesCompletos),
            alcance.Contratos ? g.Vigentes : null,
            alcance.Contratos ? g.Totales : null));
    }

    public async Task<GaranteDetalleViewModel?> ObtenerDetalleAsync(int id, AlcanceDatos alcance, CancellationToken ct = default)
    {
        var garante = await _db.Garantes.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, ct);
        if (garante is null)
        {
            return null;
        }

        // Sin permiso, los contratos ni siquiera se consultan (null = "no disponible para tu rol").
        IReadOnlyList<ContratoGarantizadoViewModel>? contratos = null;
        if (alcance.Contratos)
        {
            var garantias = await _db.ContratosGarantes.AsNoTracking()
                .Where(cg => cg.GaranteId == id)
                .ToDictionaryAsync(cg => cg.ContratoId, cg => new { cg.TipoGarantia, cg.Detalle }, ct);

            var resumenes = await _contratos.ObtenerAsync(c => c.Garantes.Any(cg => cg.GaranteId == id), ct);
            contratos = resumenes
                .Where(c => garantias.ContainsKey(c.Id))
                .Select(c => new ContratoGarantizadoViewModel(c, garantias[c.Id].TipoGarantia, garantias[c.Id].Detalle))
                .ToList();
        }

        return new GaranteDetalleViewModel(
            garante.Id,
            PersonaMapeo.ADatos(garante, alcance.DatosPersonalesCompletos),
            contratos);
    }

    public async Task<GaranteFormViewModel?> ObtenerParaEditarAsync(int id, CancellationToken ct = default)
    {
        var garante = await _db.Garantes.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, ct);
        return garante is null ? null : PersonaMapeo.AFormulario<GaranteFormViewModel>(garante);
    }

    public async Task<ResultadoOperacion> CrearAsync(GaranteFormViewModel modelo, CancellationToken ct = default)
    {
        if (await ExisteDocumentoAsync(modelo, excluirId: null, ct))
        {
            return ResultadoOperacion.Fallo(MensajeDocumentoDuplicado, nameof(modelo.NumeroDocumento));
        }

        var garante = new Garante();
        PersonaMapeo.CopiarA(modelo, garante);
        _db.Garantes.Add(garante);

        return await GuardarAsync(ct) ?? ResultadoOperacion.Ok(garante.Id);
    }

    public async Task<ResultadoOperacion> ActualizarAsync(int id, GaranteFormViewModel modelo, CancellationToken ct = default)
    {
        var garante = await _db.Garantes.FirstOrDefaultAsync(g => g.Id == id, ct);
        if (garante is null)
        {
            return ResultadoOperacion.NoEncontrado();
        }

        if (await ExisteDocumentoAsync(modelo, excluirId: id, ct))
        {
            return ResultadoOperacion.Fallo(MensajeDocumentoDuplicado, nameof(modelo.NumeroDocumento));
        }

        EstablecerVersionOriginal(garante, modelo.RowVersion);
        PersonaMapeo.CopiarA(modelo, garante);

        return await GuardarAsync(ct) ?? ResultadoOperacion.Ok(id);
    }

    public async Task<EliminarPersonaViewModel?> ObtenerParaEliminarAsync(int id, CancellationToken ct = default)
    {
        var garante = await _db.Garantes.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, ct);
        if (garante is null)
        {
            return null;
        }

        return new EliminarPersonaViewModel(
            garante.Id,
            garante.NombreCompleto,
            PersonaMapeo.MostrarDocumento(garante.TipoDocumento, garante.NumeroDocumento, completo: true),
            Convert.ToBase64String(garante.RowVersion),
            await MotivoQueImpideEliminarAsync(id, ct));
    }

    public async Task<ResultadoOperacion> EliminarAsync(int id, string? rowVersion, CancellationToken ct = default)
    {
        var garante = await _db.Garantes.FirstOrDefaultAsync(g => g.Id == id, ct);
        if (garante is null)
        {
            return ResultadoOperacion.NoEncontrado();
        }

        if (await MotivoQueImpideEliminarAsync(id, ct) is { } motivo)
        {
            return ResultadoOperacion.Fallo(motivo);
        }

        EstablecerVersionOriginal(garante, rowVersion);
        _db.Garantes.Remove(garante); // el DbContext lo convierte en borrado lógico y lo audita

        return await GuardarAsync(ct) ?? ResultadoOperacion.Ok(id);
    }

    /// <summary>Regla: no se elimina a un garante que figura en contratos (se perdería el historial).</summary>
    private async Task<string?> MotivoQueImpideEliminarAsync(int id, CancellationToken ct)
    {
        var contratos = await _db.ContratosGarantes.CountAsync(cg => cg.GaranteId == id, ct);
        return contratos == 0
            ? null
            : $"No se puede eliminar porque figura como garante en {contratos} {(contratos == 1 ? "contrato" : "contratos")}. " +
              "Los garantes con contratos se conservan para no perder el historial.";
    }

    private Task<bool> ExisteDocumentoAsync(PersonaFormViewModel modelo, int? excluirId, CancellationToken ct)
    {
        var numero = Documentos.Normalizar(modelo.NumeroDocumento);
        return _db.Garantes.AnyAsync(
            g => g.TipoDocumento == modelo.TipoDocumento && g.NumeroDocumento == numero && g.Id != excluirId, ct);
    }

    private void EstablecerVersionOriginal(Garante garante, string? rowVersion)
    {
        if (!string.IsNullOrEmpty(rowVersion))
        {
            _db.Entry(garante).Property(g => g.RowVersion).OriginalValue = Convert.FromBase64String(rowVersion);
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
