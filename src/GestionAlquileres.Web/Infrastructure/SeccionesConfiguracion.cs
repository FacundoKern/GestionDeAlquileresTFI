namespace GestionAlquileres.Web.Infrastructure;

/// <summary>
/// Subsecciones de Administración agrupadas bajo "Configuración". Contratos, Ajustes de precio y Bandeja
/// de mora NO están acá: son tareas operativas con acceso directo desde el menú.
/// Única fuente para el menú lateral, la ruta de navegación y la página de Configuración.
/// </summary>
public static class SeccionesConfiguracion
{
    // Índices e Impuestos y mora no se listan en el TFI: siguen "en construcción" (SeccionesProvisoriasController).
    public static readonly IReadOnlyList<GrupoConfiguracion> Grupos =
    [
        new("Inmobiliaria",
        [
            new("DatosEmpresa", "Datos de la inmobiliaria", "Nombre, contacto y datos fiscales que se muestran en el pie, el inicio y los documentos.", "bi-shop"),
        ]),
        new("Seguridad",
        [
            new("Usuarios", "Usuarios", "Alta de usuarios, roles, bloqueo y reseteo de contraseña.", "bi-person-gear"),
            new("Auditoria", "Auditoría", "Registro de cambios, accesos a datos personales e inicios de sesión.", "bi-journal-text"),
        ]),
    ];

    /// <summary>Controladores que marcan "Configuración" como sección activa (incluye la página índice).</summary>
    public static readonly string Controladores =
        string.Join(",", Grupos.SelectMany(g => g.Secciones).Select(s => s.Controlador).Prepend("Inicio"));

    public static bool EsSubseccion(string? controlador) =>
        Grupos.SelectMany(g => g.Secciones).Any(s => string.Equals(s.Controlador, controlador, StringComparison.OrdinalIgnoreCase));
}

public record GrupoConfiguracion(string Titulo, IReadOnlyList<SeccionConfiguracion> Secciones);

public record SeccionConfiguracion(string Controlador, string Titulo, string Descripcion, string Icono);
