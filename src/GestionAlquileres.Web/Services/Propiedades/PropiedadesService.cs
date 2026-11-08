using GestionAlquileres.Web.Data;
using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Models.Base;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Models.Enums;
using GestionAlquileres.Web.Services.Comun;
using GestionAlquileres.Web.Services.Contratos;
using GestionAlquileres.Web.ViewModels.Comun;
using GestionAlquileres.Web.ViewModels.Propiedades;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Web.Services.Propiedades;

public interface IPropiedadesService
{
    Task<ListaPaginada<PropiedadFilaViewModel>> ListarAsync(FiltroPropiedades filtro, CancellationToken ct = default);

    Task<PropiedadDetalleViewModel?> ObtenerDetalleAsync(int id, AlcanceDatos alcance, CancellationToken ct = default);

    /// <summary>Formulario de alta, opcionalmente con un propietario ya elegido (al 100 %).</summary>
    Task<PropiedadFormViewModel> NuevoFormularioAsync(int? propietarioId, CancellationToken ct = default);

    Task<PropiedadFormViewModel?> ObtenerParaEditarAsync(int id, CancellationToken ct = default);

    /// <summary>Vuelve a cargar las opciones de los desplegables (cuando el formulario vuelve con errores).</summary>
    Task PrepararFormularioAsync(PropiedadFormViewModel modelo, CancellationToken ct = default);

    Task<ResultadoOperacion> CrearAsync(PropiedadFormViewModel modelo, CancellationToken ct = default);

    Task<ResultadoOperacion> ActualizarAsync(int id, PropiedadFormViewModel modelo, CancellationToken ct = default);

    Task<EliminarPropiedadViewModel?> ObtenerParaEliminarAsync(int id, CancellationToken ct = default);

    Task<ResultadoOperacion> EliminarAsync(int id, string? rowVersion, CancellationToken ct = default);
}

public sealed class PropiedadesService : IPropiedadesService
{
    private readonly AppDbContext _db;
    private readonly IResumenContratosService _contratos;

    public PropiedadesService(AppDbContext db, IResumenContratosService contratos)
    {
        _db = db;
        _contratos = contratos;
    }

    public async Task<ListaPaginada<PropiedadFilaViewModel>> ListarAsync(FiltroPropiedades filtro, CancellationToken ct = default)
    {
        var consulta = _db.Propiedades.AsNoTracking();

        foreach (var palabra in BusquedaTexto.Palabras(filtro.Texto))
        {
            consulta = consulta.Where(p =>
                EF.Functions.Collate(p.Calle, BusquedaTexto.SinAcentos).Contains(palabra)
                || p.Numero.Contains(palabra)
                || EF.Functions.Collate(p.Localidad, BusquedaTexto.SinAcentos).Contains(palabra));
        }

        foreach (var palabra in BusquedaTexto.Palabras(filtro.Propietario))
        {
            var digitos = Documentos.Normalizar(palabra);
            consulta = consulta.Where(p => p.Propietarios.Any(pp =>
                EF.Functions.Collate(pp.Propietario.Nombre, BusquedaTexto.SinAcentos).Contains(palabra)
                || EF.Functions.Collate(pp.Propietario.Apellido!, BusquedaTexto.SinAcentos).Contains(palabra)
                || (digitos.Length >= 3 && pp.Propietario.NumeroDocumento.Contains(digitos))));
        }

        if (filtro.Tipo is { } tipo)
        {
            consulta = consulta.Where(p => p.Tipo == tipo);
        }

        consulta = filtro.Estado switch
        {
            EstadoPropiedad.Inactiva => consulta.Where(p => !p.Activa),
            EstadoPropiedad.Alquilada => consulta.Where(p => p.Activa && p.Contratos.Any(c => c.Estado == EstadoContrato.Vigente)),
            EstadoPropiedad.Disponible => consulta.Where(p => p.Activa && !p.Contratos.Any(c => c.Estado == EstadoContrato.Vigente)),
            _ => consulta,
        };

        var pagina = await consulta
            .OrderBy(p => p.Calle).ThenBy(p => p.Numero).ThenBy(p => p.Piso).ThenBy(p => p.Departamento)
            .Select(p => new
            {
                p.Id,
                p.Calle,
                p.Numero,
                p.Piso,
                p.Departamento,
                p.Localidad,
                p.Tipo,
                p.Activa,
                TieneContratoVigente = p.Contratos.Any(c => c.Estado == EstadoContrato.Vigente),
                Propietarios = p.Propietarios
                    .OrderByDescending(pp => pp.Porcentaje)
                    .Select(pp => new { pp.Propietario.TipoPersona, pp.Propietario.Nombre, pp.Propietario.Apellido })
                    .ToList(),
                FotoPrincipalId = p.Fotos.Where(f => f.EsPrincipal).Select(f => (int?)f.Id).FirstOrDefault(),
            })
            .ToListaPaginadaAsync(filtro.Pagina, ct: ct);

        return pagina.Mapear(p => new PropiedadFilaViewModel(
            p.Id,
            Propiedad.FormatearDireccion(p.Calle, p.Numero, p.Piso, p.Departamento, p.Localidad),
            p.Tipo,
            p.Propietarios.Count == 0
                ? "Sin propietarios"
                : Persona.FormatearNombre(p.Propietarios[0].TipoPersona, p.Propietarios[0].Nombre, p.Propietarios[0].Apellido)
                  + (p.Propietarios.Count > 1 ? $" y {p.Propietarios.Count - 1} más" : string.Empty),
            Propiedad.CalcularEstado(p.Activa, p.TieneContratoVigente),
            p.FotoPrincipalId));
    }

    public async Task<PropiedadDetalleViewModel?> ObtenerDetalleAsync(int id, AlcanceDatos alcance, CancellationToken ct = default)
    {
        var propiedad = await _db.Propiedades.AsNoTracking()
            .Include(p => p.Propietarios).ThenInclude(pp => pp.Propietario)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (propiedad is null)
        {
            return null;
        }

        var tieneContratoVigente = await _db.Contratos.AnyAsync(c => c.PropiedadId == id && c.Estado == EstadoContrato.Vigente, ct);

        // Sin permiso, los contratos ni siquiera se consultan (null = "no disponible para tu rol").
        var contratos = alcance.Contratos ? await _contratos.ObtenerAsync(c => c.PropiedadId == id, ct) : null;

        return new PropiedadDetalleViewModel(
            propiedad.Id,
            propiedad.Direccion,
            propiedad.Calle,
            propiedad.Numero,
            propiedad.Piso,
            propiedad.Departamento,
            propiedad.Localidad,
            propiedad.Provincia,
            propiedad.CodigoPostal,
            propiedad.Tipo,
            Propiedad.CalcularEstado(propiedad.Activa, tieneContratoVigente),
            propiedad.Observaciones,
            propiedad.Propietarios
                .OrderByDescending(pp => pp.Porcentaje)
                .Select(pp => new PropietarioDePropiedadViewModel(pp.PropietarioId, pp.Propietario.NombreCompleto, pp.Porcentaje))
                .ToList(),
            contratos,
            propiedad.CreadoPor,
            propiedad.CreadoEnUtc,
            propiedad.ModificadoPor,
            propiedad.ModificadoEnUtc);
    }

    public async Task<PropiedadFormViewModel> NuevoFormularioAsync(int? propietarioId, CancellationToken ct = default)
    {
        var modelo = new PropiedadFormViewModel();
        if (propietarioId is { } id && await _db.Propietarios.AnyAsync(p => p.Id == id, ct))
        {
            modelo.Propietarios.Add(new PropietarioAsignadoViewModel { PropietarioId = id, Porcentaje = 100 });
        }

        await PrepararFormularioAsync(modelo, ct);
        return modelo;
    }

    public async Task<PropiedadFormViewModel?> ObtenerParaEditarAsync(int id, CancellationToken ct = default)
    {
        var propiedad = await _db.Propiedades.AsNoTracking()
            .Include(p => p.Propietarios)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (propiedad is null)
        {
            return null;
        }

        var modelo = new PropiedadFormViewModel
        {
            Id = propiedad.Id,
            Calle = propiedad.Calle,
            Numero = propiedad.Numero,
            Piso = propiedad.Piso,
            Departamento = propiedad.Departamento,
            Localidad = propiedad.Localidad,
            Provincia = propiedad.Provincia,
            CodigoPostal = propiedad.CodigoPostal,
            Tipo = propiedad.Tipo,
            Activa = propiedad.Activa,
            Observaciones = propiedad.Observaciones,
            RowVersion = Convert.ToBase64String(propiedad.RowVersion),
            Propietarios = propiedad.Propietarios
                .OrderByDescending(pp => pp.Porcentaje)
                .Select(pp => new PropietarioAsignadoViewModel { PropietarioId = pp.PropietarioId, Porcentaje = pp.Porcentaje })
                .ToList(),
        };

        await PrepararFormularioAsync(modelo, ct);
        return modelo;
    }

    public async Task PrepararFormularioAsync(PropiedadFormViewModel modelo, CancellationToken ct = default)
    {
        // TODO: si la cantidad de propietarios crece mucho, reemplazar el desplegable por un buscador.
        var propietarios = await _db.Propietarios.AsNoTracking()
            .OrderBy(p => p.Apellido ?? p.Nombre).ThenBy(p => p.Nombre)
            .Select(p => new { p.Id, p.TipoPersona, p.Nombre, p.Apellido, p.TipoDocumento, p.NumeroDocumento })
            .ToListAsync(ct);

        modelo.OpcionesPropietarios = propietarios
            .Select(p => new SelectListItem(
                $"{Persona.FormatearNombre(p.TipoPersona, p.Nombre, p.Apellido)} ({p.TipoDocumento.Nombre()} {Documentos.Formatear(p.TipoDocumento, p.NumeroDocumento)})",
                p.Id.ToString()))
            .ToList();
        modelo.CompletarFilasVacias();
    }

    public async Task<ResultadoOperacion> CrearAsync(PropiedadFormViewModel modelo, CancellationToken ct = default)
    {
        if (await ValidarPropietariosExistentesAsync(modelo, ct) is { } error)
        {
            return error;
        }

        var propiedad = new Propiedad();
        CopiarDatos(modelo, propiedad);
        foreach (var asignado in modelo.PropietariosElegidos)
        {
            propiedad.Propietarios.Add(new PropiedadPropietario { PropietarioId = asignado.PropietarioId!.Value, Porcentaje = asignado.Porcentaje!.Value });
        }

        _db.Propiedades.Add(propiedad);
        return await GuardarAsync(ct) ?? ResultadoOperacion.Ok(propiedad.Id);
    }

    public async Task<ResultadoOperacion> ActualizarAsync(int id, PropiedadFormViewModel modelo, CancellationToken ct = default)
    {
        var propiedad = await _db.Propiedades.Include(p => p.Propietarios).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (propiedad is null)
        {
            return ResultadoOperacion.NoEncontrado();
        }

        if (await ValidarPropietariosExistentesAsync(modelo, ct) is { } error)
        {
            return error;
        }

        if (propiedad.Activa && !modelo.Activa
            && await _db.Contratos.AnyAsync(c => c.PropiedadId == id && c.Estado == EstadoContrato.Vigente, ct))
        {
            return ResultadoOperacion.Fallo(
                "No se puede marcar como inactiva porque tiene un contrato vigente.", nameof(modelo.Activa));
        }

        if (!string.IsNullOrEmpty(modelo.RowVersion))
        {
            _db.Entry(propiedad).Property(p => p.RowVersion).OriginalValue = Convert.FromBase64String(modelo.RowVersion);
        }

        CopiarDatos(modelo, propiedad);
        SincronizarPropietarios(propiedad, modelo);

        // Aunque solo cambien los propietarios, la propiedad se marca como modificada: así se valida la
        // concurrencia (RowVersion) y queda registrado quién la modificó.
        var entry = _db.Entry(propiedad);
        if (entry.State == EntityState.Unchanged)
        {
            entry.State = EntityState.Modified;
        }

        return await GuardarAsync(ct) ?? ResultadoOperacion.Ok(id);
    }

    public async Task<EliminarPropiedadViewModel?> ObtenerParaEliminarAsync(int id, CancellationToken ct = default)
    {
        var propiedad = await _db.Propiedades.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return propiedad is null
            ? null
            : new EliminarPropiedadViewModel(propiedad.Id, propiedad.Direccion, Convert.ToBase64String(propiedad.RowVersion),
                await MotivoQueImpideEliminarAsync(id, ct));
    }

    public async Task<ResultadoOperacion> EliminarAsync(int id, string? rowVersion, CancellationToken ct = default)
    {
        var propiedad = await _db.Propiedades.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (propiedad is null)
        {
            return ResultadoOperacion.NoEncontrado();
        }

        if (await MotivoQueImpideEliminarAsync(id, ct) is { } motivo)
        {
            return ResultadoOperacion.Fallo(motivo);
        }

        if (!string.IsNullOrEmpty(rowVersion))
        {
            _db.Entry(propiedad).Property(p => p.RowVersion).OriginalValue = Convert.FromBase64String(rowVersion);
        }

        _db.Propiedades.Remove(propiedad); // el DbContext lo convierte en borrado lógico y lo audita
        return await GuardarAsync(ct) ?? ResultadoOperacion.Ok(id);
    }

    /// <summary>
    /// Regla: una propiedad con contratos (vigentes o históricos) no se elimina, para no perder el historial;
    /// en ese caso se marca como inactiva.
    /// </summary>
    private async Task<string?> MotivoQueImpideEliminarAsync(int id, CancellationToken ct)
    {
        var contratos = await _db.Contratos.CountAsync(c => c.PropiedadId == id, ct);
        return contratos == 0
            ? null
            : $"No se puede eliminar porque tiene {contratos} {(contratos == 1 ? "contrato registrado" : "contratos registrados")}. " +
              "Si ya no se ofrece, editala y desmarcá \"Propiedad activa\".";
    }

    /// <summary>Los propietarios elegidos tienen que existir (y no estar eliminados).</summary>
    private async Task<ResultadoOperacion?> ValidarPropietariosExistentesAsync(PropiedadFormViewModel modelo, CancellationToken ct)
    {
        var ids = modelo.PropietariosElegidos.Select(p => p.PropietarioId!.Value).Distinct().ToList();
        var existentes = await _db.Propietarios.CountAsync(p => ids.Contains(p.Id), ct);
        return existentes == ids.Count
            ? null
            : ResultadoOperacion.Fallo("Alguno de los propietarios elegidos ya no existe. Volvé a elegirlos.", nameof(modelo.Propietarios));
    }

    private static void CopiarDatos(PropiedadFormViewModel modelo, Propiedad propiedad)
    {
        propiedad.Calle = modelo.Calle.Trim();
        propiedad.Numero = modelo.Numero.Trim();
        propiedad.Piso = Limpiar(modelo.Piso);
        propiedad.Departamento = Limpiar(modelo.Departamento);
        propiedad.Localidad = modelo.Localidad.Trim();
        propiedad.Provincia = modelo.Provincia.Trim();
        propiedad.CodigoPostal = Limpiar(modelo.CodigoPostal)?.ToUpperInvariant();
        propiedad.Tipo = modelo.Tipo;
        propiedad.Activa = modelo.Activa;
        propiedad.Observaciones = Limpiar(modelo.Observaciones);
    }

    /// <summary>Agrega, actualiza o quita filas de copropiedad para que coincidan con el formulario (cada cambio se audita).</summary>
    private void SincronizarPropietarios(Propiedad propiedad, PropiedadFormViewModel modelo)
    {
        var nuevos = modelo.PropietariosElegidos.ToDictionary(p => p.PropietarioId!.Value, p => p.Porcentaje!.Value);

        foreach (var actual in propiedad.Propietarios.ToList())
        {
            if (nuevos.TryGetValue(actual.PropietarioId, out var porcentaje))
            {
                actual.Porcentaje = porcentaje;
                nuevos.Remove(actual.PropietarioId);
            }
            else
            {
                _db.PropiedadesPropietarios.Remove(actual);
            }
        }

        foreach (var (propietarioId, porcentaje) in nuevos)
        {
            propiedad.Propietarios.Add(new PropiedadPropietario { PropietarioId = propietarioId, Porcentaje = porcentaje });
        }
    }

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

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
