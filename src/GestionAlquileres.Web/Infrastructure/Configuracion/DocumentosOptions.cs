namespace GestionAlquileres.Web.Infrastructure.Configuracion;

/// <summary>PDF adjuntos a los contratos (sección "Documentos" de appsettings).</summary>
public class DocumentosOptions
{
    public const string Seccion = "Documentos";

    /// <summary>
    /// Carpeta donde se guardan los archivos, FUERA de wwwroot (solo se descargan con sesión de Administrador).
    /// Relativa a la carpeta de la aplicación o absoluta. En producción tiene que estar incluida en los backups.
    /// </summary>
    public string Carpeta { get; set; } = "App_Data/documentos";

    /// <summary>Tamaño máximo de cada PDF, en MB (un contrato escaneado suele pesar 1 a 5 MB).</summary>
    public int TamanioMaximoMB { get; set; } = 10;

    public long TamanioMaximoBytes => TamanioMaximoMB * 1024L * 1024L;
}
