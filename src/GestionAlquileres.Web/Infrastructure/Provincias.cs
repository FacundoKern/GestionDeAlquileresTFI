namespace GestionAlquileres.Web.Infrastructure;

/// <summary>Las 24 jurisdicciones de Argentina, para elegir de una lista en lugar de escribirlas.</summary>
public static class Provincias
{
    public const string Caba = "Ciudad Autónoma de Buenos Aires";

    public static readonly IReadOnlyList<string> Todas =
    [
        Caba,
        "Buenos Aires",
        "Catamarca",
        "Chaco",
        "Chubut",
        "Córdoba",
        "Corrientes",
        "Entre Ríos",
        "Formosa",
        "Jujuy",
        "La Pampa",
        "La Rioja",
        "Mendoza",
        "Misiones",
        "Neuquén",
        "Río Negro",
        "Salta",
        "San Juan",
        "San Luis",
        "Santa Cruz",
        "Santa Fe",
        "Santiago del Estero",
        "Tierra del Fuego",
        "Tucumán",
    ];
}
