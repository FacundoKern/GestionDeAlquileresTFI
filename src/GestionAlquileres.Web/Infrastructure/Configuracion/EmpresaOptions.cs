namespace GestionAlquileres.Web.Infrastructure.Configuracion;

/// <summary>
/// Identidad visual (sección "Empresa" de appsettings): rutas dentro de wwwroot. Si un archivo no existe, no
/// se muestra (el sistema funciona sin logo). Los datos del negocio (nombre, contacto, CUIT) no van acá: se
/// editan en Administración → Datos de la inmobiliaria.
/// </summary>
public class EmpresaOptions
{
    public const string Seccion = "Empresa";

    /// <summary>Logo a color, para fondos claros (inicio de sesión, inicio, documentos). P. ej. "img/logo.png".</summary>
    public string? Logo { get; set; }

    /// <summary>Versión para la barra superior oscura (texto claro). Vacío = se usa <see cref="Logo"/>.</summary>
    public string? LogoSobreOscuro { get; set; }

    /// <summary>Ícono de la pestaña del navegador (PNG de 32×32).</summary>
    public string? Icono { get; set; }

    /// <summary>Ícono para accesos directos en celulares (PNG de 180×180).</summary>
    public string? IconoApple { get; set; }
}
