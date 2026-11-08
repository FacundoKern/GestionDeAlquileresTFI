namespace GestionAlquileres.Web.Security;

public static class Roles
{
    /// <summary>Acceso total, incluida la sección Administración.</summary>
    public const string Administrador = "Administrador";

    /// <summary>Gestiona inquilinos, propietarios, propiedades, garantes, pagos y reportes. Sin Administración.</summary>
    public const string Operador = "Operador";

    /// <summary>Solo lectura. Ve los documentos enmascarados.</summary>
    public const string Consulta = "Consulta";

    public static readonly IReadOnlyList<string> Todos = [Administrador, Operador, Consulta];

    /// <summary>Qué puede hacer cada rol, en palabras simples (para el formulario de usuarios).</summary>
    public static string Descripcion(string rol) => rol switch
    {
        Administrador => "Acceso total, incluida la sección Administración (contratos, configuración, usuarios y auditoría).",
        Operador => "Carga y modifica inquilinos, propietarios, propiedades, garantes y pagos. No entra a Administración.",
        Consulta => "Solo lectura. Ve los datos personales parcialmente ocultos y no ve información de contratos.",
        _ => string.Empty,
    };
}

/// <summary>
/// Políticas de autorización. Se aplican en los controladores con [Authorize(Policy = ...)];
/// la UI solo oculta lo que el rol no puede usar, la protección real está en el servidor.
/// </summary>
public static class Politicas
{
    /// <summary>Cualquier rol del sistema: ver listados y fichas.</summary>
    public const string Lectura = "Lectura";

    /// <summary>Administrador y Operador: crear, editar y eliminar datos operativos.</summary>
    public const string Gestion = "Gestion";

    /// <summary>Solo Administrador: sección Administración (contratos, ajustes, mora, índices, usuarios, auditoría).</summary>
    public const string Administracion = "Administracion";

    /// <summary>Ver documento, teléfono y email completos. Consulta los ve parcialmente ocultos.</summary>
    public const string DatosPersonalesCompletos = "DatosPersonalesCompletos";

    /// <summary>
    /// Ver información de contratos (en fichas y en el tablero de inicio). Consulta no la ve.
    /// La ficha completa del contrato (fase 2) es solo para Administrador (política Administracion).
    /// </summary>
    public const string InformacionContratos = "InformacionContratos";
}

public static class ClaimsPropios
{
    public const string NombreCompleto = "ga:nombre_completo";
    public const string DebeCambiarClave = "ga:debe_cambiar_clave";
}
