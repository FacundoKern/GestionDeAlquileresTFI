using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Web.ViewModels.Comun;

public class ListaPaginada<T>
{
    public const int TamanioPorDefecto = 20;

    public ListaPaginada(IReadOnlyList<T> items, int pagina, int tamanioPagina, int totalRegistros)
    {
        Items = items;
        Pagina = pagina;
        TamanioPagina = tamanioPagina;
        TotalRegistros = totalRegistros;
    }

    public IReadOnlyList<T> Items { get; }

    public int Pagina { get; }

    public int TamanioPagina { get; }

    public int TotalRegistros { get; }

    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(TotalRegistros / (double)TamanioPagina));

    /// <summary>Convierte los ítems de la página (p. ej. para formatear o enmascarar en memoria).</summary>
    public ListaPaginada<TDestino> Mapear<TDestino>(Func<T, TDestino> conversion) =>
        new(Items.Select(conversion).ToList(), Pagina, TamanioPagina, TotalRegistros);
}

public static class ListaPaginadaExtensions
{
    public static async Task<ListaPaginada<T>> ToListaPaginadaAsync<T>(
        this IQueryable<T> consulta,
        int pagina,
        int tamanioPagina = ListaPaginada<T>.TamanioPorDefecto,
        CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        var total = await consulta.CountAsync(ct);
        var items = await consulta.Skip((pagina - 1) * tamanioPagina).Take(tamanioPagina).ToListAsync(ct);
        return new ListaPaginada<T>(items, pagina, tamanioPagina, total);
    }
}

/// <summary>Datos para el parcial _Paginacion: conserva los filtros actuales en los enlaces.</summary>
public record PaginacionViewModel(int Pagina, int TotalPaginas, int TotalRegistros, IReadOnlyDictionary<string, string?> Filtros);
