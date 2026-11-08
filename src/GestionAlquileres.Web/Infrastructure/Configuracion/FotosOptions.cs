namespace GestionAlquileres.Web.Infrastructure.Configuracion;

/// <summary>Fotos de las propiedades (sección "Fotos" de appsettings).</summary>
public class FotosOptions
{
    public const string Seccion = "Fotos";

    /// <summary>
    /// Carpeta donde se guardan los archivos, FUERA de wwwroot (solo se sirven a usuarios con sesión).
    /// Relativa a la carpeta de la aplicación o absoluta. En producción tiene que estar incluida en los backups.
    /// </summary>
    public string Carpeta { get; set; } = "App_Data/fotos";

    public int MaximoPorPropiedad { get; set; } = 30;

    public int MaximoPorEnvio { get; set; } = 10;

    /// <summary>Tamaño máximo de cada archivo subido, en MB (las fotos de celular suelen pesar 2 a 6 MB).</summary>
    public int TamanioMaximoMB { get; set; } = 10;

    /// <summary>Resolución máxima aceptada, en megapíxeles: protege contra imágenes diseñadas para agotar la memoria.</summary>
    public int MegapixelesMaximos { get; set; } = 50;

    /// <summary>Lado mayor de la foto guardada, en píxeles.</summary>
    public int LadoMaximo { get; set; } = 1600;

    /// <summary>Lado mayor de la miniatura, en píxeles.</summary>
    public int LadoMiniatura { get; set; } = 400;

    public long TamanioMaximoBytes => TamanioMaximoMB * 1024L * 1024L;
}
