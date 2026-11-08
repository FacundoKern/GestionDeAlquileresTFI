namespace GestionAlquileres.Web.Data;

/// <summary>
/// Búsqueda de texto insensible a mayúsculas y a acentos ("gonzalez" encuentra "González").
/// Uso dentro de la consulta (EF no traduce métodos propios, debe escribirse en línea):
/// <code>EF.Functions.Collate(p.Nombre, BusquedaTexto.SinAcentos).Contains(palabra)</code>
/// Para volúmenes grandes conviene una columna normalizada indexada o búsqueda de texto completo.
/// </summary>
public static class BusquedaTexto
{
    public const string SinAcentos = "Modern_Spanish_CI_AI";

    /// <summary>Divide la búsqueda en palabras (cada palabra debe coincidir en algún campo). Máximo 5.</summary>
    public static string[] Palabras(string? texto) =>
        (texto ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(5)
            .ToArray();
}
