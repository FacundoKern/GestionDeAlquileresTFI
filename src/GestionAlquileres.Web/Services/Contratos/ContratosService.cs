using GestionAlquileres.Web.Data;
using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Infrastructure.Configuracion;
using GestionAlquileres.Web.Models.Base;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Models.Enums;
using GestionAlquileres.Web.Security;
using GestionAlquileres.Web.Services.Comun;
using GestionAlquileres.Web.ViewModels.Comun;
using GestionAlquileres.Web.ViewModels.Contratos;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GestionAlquileres.Web.Services.Contratos;

/// <summary>
/// ABM mínimo de contratos para el TFI: alta, listado, ficha, rescisión y PDF adjuntos.
/// Los ajustes de precio, las cuotas y los pagos quedan para las fases siguientes del sistema real.
/// </summary>
public interface IContratosService
{
    Task<ListaPaginada<ContratoResumenViewModel>> ListarAsync(FiltroContratos filtro, CancellationToken ct = default);

    Task<ContratoDetalleViewModel?> ObtenerDetalleAsync(int id, CancellationToken ct = default);

    /// <summary>Formulario de alta, opcionalmente con la propiedad ya elegida (viene de su ficha).</summary>
    Task<ContratoFormViewModel> NuevoFormularioAsync(int? propiedadId, CancellationToken ct = default);

    Task PrepararFormularioAsync(ContratoFormViewModel modelo, CancellationToken ct = default);

    /// <summary>Crea el contrato. Si hay algo para advertir sin bloquear (USD ajustado por ICL), viene en <c>Advertencia</c>.</summary>
    Task<(ResultadoOperacion Resultado, string? Advertencia)> CrearAsync(ContratoFormViewModel modelo, CancellationToken ct = default);

    Task<RescindirContratoViewModel?> ObtenerParaRescindirAsync(int id, CancellationToken ct = default);

    Task<ResultadoOperacion> RescindirAsync(int id, RescindirContratoViewModel modelo, CancellationToken ct = default);

    Task<ResultadoOperacion> SubirDocumentoAsync(int contratoId, IFormFile? archivo, string? descripcion, CancellationToken ct = default);

    /// <summary>Ruta física del PDF y el nombre con el que se descarga, o null si no existe.</summary>
    Task<(string Ruta, string NombreDescarga)?> ObtenerDocumentoAsync(int id, CancellationToken ct = default);

    Task<EliminarDocumentoViewModel?> ObtenerParaEliminarDocumentoAsync(int id, CancellationToken ct = default);

    /// <summary>Borrado lógico del documento (el archivo se conserva). Devuelve el contrato para volver a su ficha.</summary>
    Task<(ResultadoOperacion Resultado, int? ContratoId)> EliminarDocumentoAsync(int id, string? rowVersion, CancellationToken ct = default);
}

public sealed class ContratosService : IContratosService
{
    private const string CodigoIcl = "ICL";

    private readonly AppDbContext _db;
    private readonly IResumenContratosService _resumen;
    private readonly IAlmacenDocumentos _almacen;
    private readonly IUsuarioActual _usuario;
    private readonly TimeProvider _reloj;
    private readonly DocumentosOptions _opcionesDocumentos;

    public ContratosService(
        AppDbContext db,
        IResumenContratosService resumen,
        IAlmacenDocumentos almacen,
        IUsuarioActual usuario,
        TimeProvider reloj,
        IOptions<DocumentosOptions> opcionesDocumentos)
    {
        _db = db;
        _resumen = resumen;
        _almacen = almacen;
        _usuario = usuario;
        _reloj = reloj;
        _opcionesDocumentos = opcionesDocumentos.Value;
    }

    public async Task<ListaPaginada<ContratoResumenViewModel>> ListarAsync(FiltroContratos filtro, CancellationToken ct = default)
    {
        var palabras = BusquedaTexto.Palabras(filtro.Texto);
        var contratos = await _resumen.FiltrarAsync(consulta =>
        {
            foreach (var palabra in palabras)
            {
                consulta = consulta.Where(c =>
                    EF.Functions.Collate(c.Propiedad.Calle, BusquedaTexto.SinAcentos).Contains(palabra)
                    || c.Propiedad.Numero.Contains(palabra)
                    || EF.Functions.Collate(c.Propiedad.Localidad, BusquedaTexto.SinAcentos).Contains(palabra)
                    || c.Inquilinos.Any(i =>
                        EF.Functions.Collate(i.Inquilino.Nombre, BusquedaTexto.SinAcentos).Contains(palabra)
                        || EF.Functions.Collate(i.Inquilino.Apellido!, BusquedaTexto.SinAcentos).Contains(palabra)));
            }

            return filtro.Estado is { } estado ? consulta.Where(c => c.Estado == estado) : consulta;
        }, ct);

        // El semáforo se calcula en memoria (CalculadoraSemaforo): la página se arma después. Son pocos contratos.
        var pagina = Math.Max(1, filtro.Pagina);
        var tamanio = ListaPaginada<ContratoResumenViewModel>.TamanioPorDefecto;
        return new ListaPaginada<ContratoResumenViewModel>(
            contratos.Skip((pagina - 1) * tamanio).Take(tamanio).ToList(), pagina, tamanio, contratos.Count);
    }

    public async Task<ContratoDetalleViewModel?> ObtenerDetalleAsync(int id, CancellationToken ct = default)
    {
        var contrato = await _db.Contratos.AsNoTracking()
            .Include(c => c.Propiedad)
            .Include(c => c.Inquilinos).ThenInclude(i => i.Inquilino)
            .Include(c => c.Garantes).ThenInclude(g => g.Garante)
            .Include(c => c.ConfiguracionAjuste).ThenInclude(a => a!.Indice)
            .Include(c => c.HistorialMontos)
            .Include(c => c.Documentos)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
        if (contrato is null)
        {
            return null;
        }

        var resumen = (await _resumen.ObtenerAsync(c => c.Id == id, ct))[0];
        var ajuste = contrato.ConfiguracionAjuste;

        return new ContratoDetalleViewModel(
            contrato.Id,
            contrato.PropiedadId,
            contrato.Propiedad.Direccion,
            resumen.Estado,
            contrato.Estado,
            contrato.FechaInicio,
            contrato.FechaFin,
            Formato.Moneda(contrato.MontoInicial, contrato.Moneda),
            Formato.Moneda(contrato.MontoActual, contrato.Moneda),
            contrato.Moneda,
            contrato.DiaVencimiento,
            contrato.DepositoGarantia is { } deposito ? Formato.Moneda(deposito, contrato.Moneda) : null,
            DescribirAjuste(ajuste),
            ajuste?.ProximaFechaAjuste,
            contrato.FechaRescision,
            contrato.MotivoRescision,
            contrato.Observaciones,
            contrato.Inquilinos
                .OrderByDescending(i => i.EsTitular)
                .Select(i => new PersonaDelContratoViewModel(
                    i.InquilinoId,
                    i.Inquilino.NombreCompleto,
                    Documentos.Formatear(i.Inquilino.TipoDocumento, i.Inquilino.NumeroDocumento),
                    i.EsTitular ? "Titular" : "Coinquilino"))
                .ToList(),
            contrato.Garantes
                .Select(g => new PersonaDelContratoViewModel(
                    g.GaranteId,
                    g.Garante.NombreCompleto,
                    Documentos.Formatear(g.Garante.TipoDocumento, g.Garante.NumeroDocumento),
                    g.TipoGarantia.Nombre() + (string.IsNullOrWhiteSpace(g.Detalle) ? string.Empty : $" ({g.Detalle})")))
                .ToList(),
            contrato.HistorialMontos
                .OrderBy(h => h.FechaVigencia).ThenBy(h => h.Id)
                .Select(h => new HistorialMontoViewModel(
                    h.FechaVigencia,
                    h.MontoAnterior == 0 ? "—" : Formato.Moneda(h.MontoAnterior, contrato.Moneda),
                    Formato.Moneda(h.MontoNuevo, contrato.Moneda),
                    h.Observaciones ?? h.TipoAjuste?.Nombre() ?? "Ajuste"))
                .ToList(),
            contrato.Documentos
                .OrderBy(d => d.CreadoEnUtc)
                .Select(d => new DocumentoContratoViewModel(
                    d.Id, d.Descripcion, d.NombreOriginal, FormatearTamanio(d.TamanioBytes), d.CreadoPor, d.CreadoEnUtc))
                .ToList(),
            AdvertenciaUsdConIcl(contrato.Moneda, ajuste?.Indice?.Codigo),
            _opcionesDocumentos.TamanioMaximoMB);
    }

    public async Task<ContratoFormViewModel> NuevoFormularioAsync(int? propiedadId, CancellationToken ct = default)
    {
        var hoy = _reloj.HoyEnArgentina();
        var inicio = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(1);
        var modelo = new ContratoFormViewModel
        {
            PropiedadId = propiedadId,
            FechaInicio = inicio,
            FechaFin = inicio.AddYears(2).AddDays(-1),
        };
        await PrepararFormularioAsync(modelo, ct);
        return modelo;
    }

    public async Task PrepararFormularioAsync(ContratoFormViewModel modelo, CancellationToken ct = default)
    {
        // Solo propiedades activas sin contrato vigente: son las que se pueden alquilar.
        var propiedades = await _db.Propiedades.AsNoTracking()
            .Where(p => p.Activa && !p.Contratos.Any(c => c.Estado == EstadoContrato.Vigente))
            .OrderBy(p => p.Calle).ThenBy(p => p.Numero).ThenBy(p => p.Piso).ThenBy(p => p.Departamento)
            .Select(p => new { p.Id, p.Calle, p.Numero, p.Piso, p.Departamento, p.Localidad })
            .ToListAsync(ct);
        modelo.OpcionesPropiedades = propiedades
            .Select(p => new SelectListItem(Propiedad.FormatearDireccion(p.Calle, p.Numero, p.Piso, p.Departamento, p.Localidad), p.Id.ToString()))
            .ToList();

        modelo.OpcionesInquilinos = await OpcionesPersonasAsync(_db.Inquilinos, ct);
        modelo.OpcionesGarantes = await OpcionesPersonasAsync(_db.Garantes, ct);
        modelo.OpcionesIndices = await _db.Indices.AsNoTracking()
            .OrderBy(i => i.Nombre)
            .Select(i => new SelectListItem(i.Nombre, i.Id.ToString()))
            .ToListAsync(ct);
    }

    public async Task<(ResultadoOperacion Resultado, string? Advertencia)> CrearAsync(ContratoFormViewModel modelo, CancellationToken ct = default)
    {
        if (await ValidarAsync(modelo, ct) is { } error)
        {
            return (error, null);
        }

        var inicio = modelo.FechaInicio!.Value;
        var contrato = new Contrato
        {
            PropiedadId = modelo.PropiedadId!.Value,
            FechaInicio = inicio,
            FechaFin = modelo.FechaFin!.Value,
            Moneda = modelo.Moneda,
            DiaVencimiento = modelo.DiaVencimiento,
            DepositoGarantia = modelo.DepositoGarantia,
            Observaciones = Limpiar(modelo.Observaciones),
            ConfiguracionAjuste = new ConfiguracionAjuste
            {
                TipoAjuste = modelo.TipoAjuste,
                IndiceId = modelo.TipoAjuste == TipoAjuste.Indice ? modelo.IndiceId : null,
                PorcentajeFijo = modelo.TipoAjuste == TipoAjuste.PorcentajeFijo ? modelo.PorcentajeFijo : null,
                PeriodicidadMeses = modelo.PeriodicidadMeses,
                ProximaFechaAjuste = inicio.AddMonths(modelo.PeriodicidadMeses),
            },
        };

        // El monto actual y su historial nacen juntos (regla del modelo: nunca se asignan por separado).
        contrato.EstablecerMontoInicial(modelo.MontoInicial!.Value, _usuario.Nombre, _reloj.AhoraUtc());

        contrato.Inquilinos.Add(new ContratoInquilino { InquilinoId = modelo.InquilinoTitularId!.Value, EsTitular = true });
        foreach (var coinquilino in (modelo.CoinquilinosIds ?? []).Distinct())
        {
            contrato.Inquilinos.Add(new ContratoInquilino { InquilinoId = coinquilino, EsTitular = false });
        }

        if (modelo.GaranteId is { } garante)
        {
            contrato.Garantes.Add(new ContratoGarante
            {
                GaranteId = garante,
                TipoGarantia = modelo.TipoGarantia!.Value,
                Detalle = Limpiar(modelo.DetalleGarantia),
            });
        }

        _db.Contratos.Add(contrato);
        await _db.SaveChangesAsync(ct);

        var codigoIndice = modelo.TipoAjuste == TipoAjuste.Indice
            ? await _db.Indices.Where(i => i.Id == modelo.IndiceId).Select(i => i.Codigo).FirstOrDefaultAsync(ct)
            : null;
        return (ResultadoOperacion.Ok(contrato.Id), AdvertenciaUsdConIcl(modelo.Moneda, codigoIndice));
    }

    public async Task<RescindirContratoViewModel?> ObtenerParaRescindirAsync(int id, CancellationToken ct = default)
    {
        var contrato = await _db.Contratos.AsNoTracking().Include(c => c.Propiedad).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (contrato is null)
        {
            return null;
        }

        var hoy = _reloj.HoyEnArgentina();
        return new RescindirContratoViewModel
        {
            Id = contrato.Id,
            Propiedad = contrato.Propiedad.Direccion,
            FechaInicio = contrato.FechaInicio,
            FechaFin = contrato.FechaFin,
            FechaRescision = hoy < contrato.FechaInicio ? contrato.FechaInicio : hoy > contrato.FechaFin ? contrato.FechaFin : hoy,
            RowVersion = Convert.ToBase64String(contrato.RowVersion),
        };
    }

    public async Task<ResultadoOperacion> RescindirAsync(int id, RescindirContratoViewModel modelo, CancellationToken ct = default)
    {
        var contrato = await _db.Contratos.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (contrato is null)
        {
            return ResultadoOperacion.NoEncontrado();
        }

        if (contrato.Estado != EstadoContrato.Vigente)
        {
            return ResultadoOperacion.Fallo($"El contrato ya está {contrato.Estado.Nombre().ToLowerInvariant()}: solo se rescinde un contrato vigente.");
        }

        var fecha = modelo.FechaRescision!.Value;
        if (fecha < contrato.FechaInicio || fecha > contrato.FechaFin)
        {
            return ResultadoOperacion.Fallo(
                $"La fecha tiene que estar dentro de la vigencia del contrato ({Formato.Fecha(contrato.FechaInicio)} al {Formato.Fecha(contrato.FechaFin)}).",
                nameof(modelo.FechaRescision));
        }

        if (!string.IsNullOrEmpty(modelo.RowVersion))
        {
            _db.Entry(contrato).Property(c => c.RowVersion).OriginalValue = Convert.FromBase64String(modelo.RowVersion);
        }

        contrato.Estado = EstadoContrato.Rescindido;
        contrato.FechaRescision = fecha;
        contrato.MotivoRescision = modelo.Motivo!.Trim();
        return await GuardarAsync(ct) ?? ResultadoOperacion.Ok(id);
    }

    public async Task<ResultadoOperacion> SubirDocumentoAsync(int contratoId, IFormFile? archivo, string? descripcion, CancellationToken ct = default)
    {
        if (!await _db.Contratos.AnyAsync(c => c.Id == contratoId, ct))
        {
            return ResultadoOperacion.NoEncontrado();
        }

        if (archivo is null || archivo.Length == 0)
        {
            return ResultadoOperacion.Fallo("Elegí el PDF que querés adjuntar.", "archivo");
        }

        if (archivo.Length > _opcionesDocumentos.TamanioMaximoBytes)
        {
            return ResultadoOperacion.Fallo($"El archivo pesa más de {_opcionesDocumentos.TamanioMaximoMB} MB.", "archivo");
        }

        byte[] contenido;
        using (var memoria = new MemoryStream())
        {
            await archivo.CopyToAsync(memoria, ct);
            contenido = memoria.ToArray();
        }

        if (ValidadorPdf.Validar(contenido, _opcionesDocumentos.TamanioMaximoBytes) is { } motivo)
        {
            return ResultadoOperacion.Fallo(motivo, "archivo");
        }

        var nombreArchivo = Guid.NewGuid().ToString("N");
        await _almacen.GuardarAsync(nombreArchivo, contenido, ct);

        var nombreOriginal = Path.GetFileName(archivo.FileName);
        _db.DocumentosContratos.Add(new DocumentoContrato
        {
            ContratoId = contratoId,
            Archivo = nombreArchivo,
            Descripcion = Limpiar(descripcion) is { } texto ? Recortar(texto, 200) : "Contrato firmado",
            NombreOriginal = string.IsNullOrWhiteSpace(nombreOriginal) ? null : Recortar(nombreOriginal, 255),
            TamanioBytes = contenido.LongLength,
        });
        await _db.SaveChangesAsync(ct);
        return ResultadoOperacion.Ok(contratoId);
    }

    public async Task<(string Ruta, string NombreDescarga)?> ObtenerDocumentoAsync(int id, CancellationToken ct = default)
    {
        var documento = await _db.DocumentosContratos.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
        if (documento is null || _almacen.Ruta(documento.Archivo) is not { } ruta)
        {
            return null;
        }

        // El nombre de descarga lo arma el sistema: no se reenvía el nombre que eligió quien lo subió.
        return (ruta, $"contrato-{documento.ContratoId}-documento-{documento.Id}.pdf");
    }

    public async Task<EliminarDocumentoViewModel?> ObtenerParaEliminarDocumentoAsync(int id, CancellationToken ct = default)
    {
        var documento = await _db.DocumentosContratos.AsNoTracking()
            .Include(d => d.Contrato).ThenInclude(c => c.Propiedad)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
        return documento is null
            ? null
            : new EliminarDocumentoViewModel(
                documento.Id, documento.ContratoId, documento.Descripcion, documento.Contrato.Propiedad.Direccion,
                Convert.ToBase64String(documento.RowVersion));
    }

    public async Task<(ResultadoOperacion Resultado, int? ContratoId)> EliminarDocumentoAsync(int id, string? rowVersion, CancellationToken ct = default)
    {
        var documento = await _db.DocumentosContratos.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (documento is null)
        {
            return (ResultadoOperacion.NoEncontrado(), null);
        }

        if (!string.IsNullOrEmpty(rowVersion))
        {
            _db.Entry(documento).Property(d => d.RowVersion).OriginalValue = Convert.FromBase64String(rowVersion);
        }

        _db.DocumentosContratos.Remove(documento); // borrado lógico y auditado; el archivo se conserva
        return (await GuardarAsync(ct) ?? ResultadoOperacion.Ok(id), documento.ContratoId);
    }

    /// <summary>Reglas que necesitan la base: que lo elegido exista y que la propiedad esté libre en esas fechas.</summary>
    private async Task<ResultadoOperacion?> ValidarAsync(ContratoFormViewModel modelo, CancellationToken ct)
    {
        var propiedad = await _db.Propiedades.AsNoTracking()
            .Where(p => p.Id == modelo.PropiedadId)
            .Select(p => new { p.Activa })
            .FirstOrDefaultAsync(ct);
        if (propiedad is null)
        {
            return ResultadoOperacion.Fallo("La propiedad elegida ya no existe.", nameof(modelo.PropiedadId));
        }

        if (!propiedad.Activa)
        {
            return ResultadoOperacion.Fallo("La propiedad está inactiva: activala desde su ficha antes de alquilarla.", nameof(modelo.PropiedadId));
        }

        var inicio = modelo.FechaInicio!.Value;
        var fin = modelo.FechaFin!.Value;
        var superpuesto = await _db.Contratos.AnyAsync(c =>
            c.PropiedadId == modelo.PropiedadId
            && c.Estado == EstadoContrato.Vigente
            && c.FechaInicio <= fin
            && c.FechaFin >= inicio, ct);
        if (superpuesto)
        {
            return ResultadoOperacion.Fallo("La propiedad ya tiene un contrato vigente en esas fechas.", nameof(modelo.PropiedadId));
        }

        var inquilinos = (modelo.CoinquilinosIds ?? []).Append(modelo.InquilinoTitularId!.Value).Distinct().ToList();
        if (await _db.Inquilinos.CountAsync(i => inquilinos.Contains(i.Id), ct) != inquilinos.Count)
        {
            return ResultadoOperacion.Fallo("Alguno de los inquilinos elegidos ya no existe. Volvé a elegirlos.", nameof(modelo.InquilinoTitularId));
        }

        if (modelo.GaranteId is { } garante && !await _db.Garantes.AnyAsync(g => g.Id == garante, ct))
        {
            return ResultadoOperacion.Fallo("El garante elegido ya no existe.", nameof(modelo.GaranteId));
        }

        if (modelo.TipoAjuste == TipoAjuste.Indice && !await _db.Indices.AnyAsync(i => i.Id == modelo.IndiceId, ct))
        {
            return ResultadoOperacion.Fallo("El índice elegido ya no existe.", nameof(modelo.IndiceId));
        }

        return null;
    }

    private static async Task<IReadOnlyList<SelectListItem>> OpcionesPersonasAsync<T>(IQueryable<T> personas, CancellationToken ct)
        where T : Persona
    {
        var lista = await personas.AsNoTracking()
            .OrderBy(p => p.Apellido ?? p.Nombre).ThenBy(p => p.Nombre)
            .Select(p => new { p.Id, p.TipoPersona, p.Nombre, p.Apellido, p.TipoDocumento, p.NumeroDocumento })
            .ToListAsync(ct);
        return lista
            .Select(p => new SelectListItem(
                $"{Persona.FormatearNombre(p.TipoPersona, p.Nombre, p.Apellido)} ({Documentos.Formatear(p.TipoDocumento, p.NumeroDocumento)})",
                p.Id.ToString()))
            .ToList();
    }

    private static string DescribirAjuste(ConfiguracionAjuste? ajuste) => ajuste switch
    {
        null => "Sin ajuste configurado",
        { TipoAjuste: TipoAjuste.Indice } => $"Por {ajuste.Indice?.Codigo ?? "índice"}, cada {Meses(ajuste.PeriodicidadMeses)}",
        { TipoAjuste: TipoAjuste.PorcentajeFijo } => $"{Formato.Porcentaje(ajuste.PorcentajeFijo ?? 0)} cada {Meses(ajuste.PeriodicidadMeses)}",
        _ => $"Manual, cada {Meses(ajuste.PeriodicidadMeses)}",
    };

    private static string Meses(int meses) => meses == 1 ? "mes" : $"{meses} meses";

    /// <summary>Decisión del usuario: un contrato en dólares ajustado por ICL se advierte, pero no se bloquea.</summary>
    private static string? AdvertenciaUsdConIcl(Moneda moneda, string? codigoIndice) =>
        moneda == Moneda.Usd && codigoIndice == CodigoIcl
            ? "El contrato está en dólares y se ajusta por ICL, un índice pensado para contratos en pesos. Revisá que sea lo acordado."
            : null;

    private static string FormatearTamanio(long bytes) =>
        bytes >= 1024 * 1024 ? $"{Formato.Numero(bytes / 1024m / 1024m, 1)} MB" : $"{Math.Max(1, bytes / 1024)} KB";

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string Recortar(string valor, int maximo) => valor.Length <= maximo ? valor : valor[..maximo];

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
    }
}
