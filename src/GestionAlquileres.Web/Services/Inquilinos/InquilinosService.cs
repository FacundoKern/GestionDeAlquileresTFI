using GestionAlquileres.Web.Data;
using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Models.Enums;
using GestionAlquileres.Web.Services.Comun;
using GestionAlquileres.Web.Services.Contratos;
using GestionAlquileres.Web.Services.Personas;
using GestionAlquileres.Web.ViewModels.Comun;
using GestionAlquileres.Web.ViewModels.Contratos;
using GestionAlquileres.Web.ViewModels.Inquilinos;
using GestionAlquileres.Web.ViewModels.Personas;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Web.Services.Inquilinos;

public interface IInquilinosService
{
    Task<ListaPaginada<InquilinoFilaViewModel>> ListarAsync(FiltroInquilinos filtro, AlcanceDatos alcance, CancellationToken ct = default);

    Task<InquilinoDetalleViewModel?> ObtenerDetalleAsync(int id, AlcanceDatos alcance, CancellationToken ct = default);

    Task<InquilinoFormViewModel?> ObtenerParaEditarAsync(int id, CancellationToken ct = default);

    Task<ResultadoOperacion> CrearAsync(InquilinoFormViewModel modelo, CancellationToken ct = default);

    Task<ResultadoOperacion> ActualizarAsync(int id, InquilinoFormViewModel modelo, CancellationToken ct = default);

    Task<EliminarPersonaViewModel?> ObtenerParaEliminarAsync(int id, CancellationToken ct = default);

    Task<ResultadoOperacion> EliminarAsync(int id, string? rowVersion, CancellationToken ct = default);
}

public sealed class InquilinosService : IInquilinosService
{
    private const string MensajeDocumentoDuplicado = "Ya existe un inquilino con ese tipo y número de documento.";

    private readonly AppDbContext _db;
    private readonly IResumenContratosService _contratos;

    public InquilinosService(AppDbContext db, IResumenContratosService contratos)
    {
        _db = db;
        _contratos = contratos;
    }

    public async Task<ListaPaginada<InquilinoFilaViewModel>> ListarAsync(
        FiltroInquilinos filtro,
        AlcanceDatos alcance,
        CancellationToken ct = default)
    {
        var consulta = _db.Inquilinos.AsNoTracking();

        foreach (var palabra in BusquedaTexto.Palabras(filtro.Texto))
        {
            var digitos = Documentos.Normalizar(palabra);
            consulta = consulta.Where(i =>
                EF.Functions.Collate(i.Nombre, BusquedaTexto.SinAcentos).Contains(palabra)
                || EF.Functions.Collate(i.Apellido!, BusquedaTexto.SinAcentos).Contains(palabra)
                || (digitos.Length >= 3 && i.NumeroDocumento.Contains(digitos)));
        }

        // Los filtros por contrato solo se aplican a los roles que pueden ver contratos: si no, por
        // descarte se podría deducir quién alquila qué.
        if (alcance.Contratos)
        {
            foreach (var palabra in BusquedaTexto.Palabras(filtro.Propiedad))
            {
                consulta = consulta.Where(i => i.Contratos.Any(ci =>
                    EF.Functions.Collate(ci.Contrato.Propiedad.Calle, BusquedaTexto.SinAcentos).Contains(palabra)
                    || ci.Contrato.Propiedad.Numero.Contains(palabra)
                    || EF.Functions.Collate(ci.Contrato.Propiedad.Localidad, BusquedaTexto.SinAcentos).Contains(palabra)));
            }

            if (filtro.ConContratoVigente is { } conVigente)
            {
                consulta = consulta.Where(i => i.Contratos.Any(ci => ci.Contrato.Estado == EstadoContrato.Vigente) == conVigente);
            }
        }

        var pagina = await consulta
            .OrderBy(i => i.Apellido ?? i.Nombre)
            .ThenBy(i => i.Nombre)
            .Select(i => new
            {
                i.Id,
                i.TipoPersona,
                i.Nombre,
                i.Apellido,
                i.TipoDocumento,
                i.NumeroDocumento,
                i.Telefono,
                i.Email,
            })
            .ToListaPaginadaAsync(filtro.Pagina, ct: ct);

        var contratos = alcance.Contratos
            ? await ObtenerContratosPrincipalesAsync(pagina.Items.Select(i => i.Id).ToList(), ct)
            : new Dictionary<int, (ContratoResumenViewModel Principal, int Otros)>();

        return pagina.Mapear(i =>
        {
            var tieneContrato = contratos.TryGetValue(i.Id, out var contrato);
            return new InquilinoFilaViewModel(
                i.Id,
                Models.Base.Persona.FormatearNombre(i.TipoPersona, i.Nombre, i.Apellido),
                PersonaMapeo.MostrarDocumento(i.TipoDocumento, i.NumeroDocumento, alcance.DatosPersonalesCompletos),
                PersonaMapeo.MostrarTelefono(i.Telefono, alcance.DatosPersonalesCompletos),
                PersonaMapeo.MostrarEmail(i.Email, alcance.DatosPersonalesCompletos),
                tieneContrato ? contrato.Principal : null,
                tieneContrato ? contrato.Otros : 0);
        });
    }

    public async Task<InquilinoDetalleViewModel?> ObtenerDetalleAsync(int id, AlcanceDatos alcance, CancellationToken ct = default)
    {
        var inquilino = await _db.Inquilinos.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct);
        if (inquilino is null)
        {
            return null;
        }

        // Sin permiso, los contratos ni siquiera se consultan (null = "no disponible para tu rol").
        var contratos = alcance.Contratos
            ? await _contratos.ObtenerAsync(c => c.Inquilinos.Any(ci => ci.InquilinoId == id), ct)
            : null;

        return new InquilinoDetalleViewModel(
            inquilino.Id,
            PersonaMapeo.ADatos(inquilino, alcance.DatosPersonalesCompletos),
            contratos);
    }

    public async Task<InquilinoFormViewModel?> ObtenerParaEditarAsync(int id, CancellationToken ct = default)
    {
        var inquilino = await _db.Inquilinos.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct);
        return inquilino is null ? null : PersonaMapeo.AFormulario<InquilinoFormViewModel>(inquilino);
    }

    public async Task<ResultadoOperacion> CrearAsync(InquilinoFormViewModel modelo, CancellationToken ct = default)
    {
        if (await ExisteDocumentoAsync(modelo, excluirId: null, ct))
        {
            return ResultadoOperacion.Fallo(MensajeDocumentoDuplicado, nameof(modelo.NumeroDocumento));
        }

        var inquilino = new Inquilino();
        PersonaMapeo.CopiarA(modelo, inquilino);
        _db.Inquilinos.Add(inquilino);

        return await GuardarAsync(ct) ?? ResultadoOperacion.Ok(inquilino.Id);
    }

    public async Task<ResultadoOperacion> ActualizarAsync(int id, InquilinoFormViewModel modelo, CancellationToken ct = default)
    {
        var inquilino = await _db.Inquilinos.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (inquilino is null)
        {
            return ResultadoOperacion.NoEncontrado();
        }

        if (await ExisteDocumentoAsync(modelo, excluirId: id, ct))
        {
            return ResultadoOperacion.Fallo(MensajeDocumentoDuplicado, nameof(modelo.NumeroDocumento));
        }

        EstablecerVersionOriginal(inquilino, modelo.RowVersion);
        PersonaMapeo.CopiarA(modelo, inquilino);

        return await GuardarAsync(ct) ?? ResultadoOperacion.Ok(id);
    }

    public async Task<EliminarPersonaViewModel?> ObtenerParaEliminarAsync(int id, CancellationToken ct = default)
    {
        var inquilino = await _db.Inquilinos.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct);
        if (inquilino is null)
        {
            return null;
        }

        return new EliminarPersonaViewModel(
            inquilino.Id,
            inquilino.NombreCompleto,
            PersonaMapeo.MostrarDocumento(inquilino.TipoDocumento, inquilino.NumeroDocumento, completo: true),
            Convert.ToBase64String(inquilino.RowVersion),
            await MotivoQueImpideEliminarAsync(id, ct));
    }

    public async Task<ResultadoOperacion> EliminarAsync(int id, string? rowVersion, CancellationToken ct = default)
    {
        var inquilino = await _db.Inquilinos.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (inquilino is null)
        {
            return ResultadoOperacion.NoEncontrado();
        }

        if (await MotivoQueImpideEliminarAsync(id, ct) is { } motivo)
        {
            return ResultadoOperacion.Fallo(motivo);
        }

        EstablecerVersionOriginal(inquilino, rowVersion);
        _db.Inquilinos.Remove(inquilino); // el DbContext lo convierte en borrado lógico y lo audita

        return await GuardarAsync(ct) ?? ResultadoOperacion.Ok(id);
    }

    /// <summary>
    /// Contrato principal de cada inquilino (el vigente o, si no tiene, el de inicio más reciente) con su
    /// semáforo, y cuántos contratos más tiene.
    /// </summary>
    private async Task<Dictionary<int, (ContratoResumenViewModel Principal, int Otros)>> ObtenerContratosPrincipalesAsync(
        List<int> inquilinoIds,
        CancellationToken ct)
    {
        var relaciones = await _db.ContratosInquilinos.AsNoTracking()
            .Where(ci => inquilinoIds.Contains(ci.InquilinoId))
            .Select(ci => new { ci.InquilinoId, ci.ContratoId, ci.Contrato.Estado, ci.Contrato.FechaInicio })
            .ToListAsync(ct);

        var principales = relaciones
            .GroupBy(r => r.InquilinoId)
            .Select(g => new
            {
                InquilinoId = g.Key,
                ContratoId = g.OrderBy(r => r.Estado == EstadoContrato.Vigente ? 0 : 1)
                    .ThenByDescending(r => r.FechaInicio)
                    .First().ContratoId,
                Otros = g.Count() - 1,
            })
            .ToList();

        var contratoIds = principales.Select(p => p.ContratoId).Distinct().ToList();
        var resumenes = contratoIds.Count == 0
            ? new Dictionary<int, ContratoResumenViewModel>()
            : (await _contratos.ObtenerAsync(c => contratoIds.Contains(c.Id), ct)).ToDictionary(c => c.Id);

        return principales
            .Where(p => resumenes.ContainsKey(p.ContratoId))
            .ToDictionary(p => p.InquilinoId, p => (resumenes[p.ContratoId], p.Otros));
    }

    /// <summary>
    /// Regla: no se elimina a un inquilino que figura en contratos (se perdería el historial de pagos,
    /// recibos y moras).
    /// </summary>
    private async Task<string?> MotivoQueImpideEliminarAsync(int id, CancellationToken ct)
    {
        var contratos = await _db.ContratosInquilinos.CountAsync(ci => ci.InquilinoId == id, ct);
        return contratos == 0
            ? null
            : $"No se puede eliminar porque figura como inquilino en {contratos} {(contratos == 1 ? "contrato" : "contratos")}. " +
              "Los inquilinos con contratos se conservan para no perder el historial.";
    }

    private Task<bool> ExisteDocumentoAsync(PersonaFormViewModel modelo, int? excluirId, CancellationToken ct)
    {
        var numero = Documentos.Normalizar(modelo.NumeroDocumento);
        return _db.Inquilinos.AnyAsync(
            i => i.TipoDocumento == modelo.TipoDocumento && i.NumeroDocumento == numero && i.Id != excluirId, ct);
    }

    private void EstablecerVersionOriginal(Inquilino inquilino, string? rowVersion)
    {
        if (!string.IsNullOrEmpty(rowVersion))
        {
            _db.Entry(inquilino).Property(i => i.RowVersion).OriginalValue = Convert.FromBase64String(rowVersion);
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
