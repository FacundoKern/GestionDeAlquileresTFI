namespace GestionAlquileres.Web.Services.Comun;

/// <summary>
/// Resultado de una operación de negocio: los servicios devuelven errores esperables (validaciones,
/// conflictos de concurrencia, reglas de negocio) en lugar de lanzar excepciones, y el controlador los
/// muestra en el formulario.
/// </summary>
public sealed record ResultadoOperacion
{
    private ResultadoOperacion(bool exito, string? error, string? campo, int? id)
    {
        Exito = exito;
        Error = error;
        Campo = campo;
        Id = id;
    }

    public bool Exito { get; }

    public string? Error { get; }

    /// <summary>Campo del formulario al que corresponde el error (null = error general).</summary>
    public string? Campo { get; }

    /// <summary>Id del registro creado (si corresponde).</summary>
    public int? Id { get; }

    public static ResultadoOperacion Ok(int? id = null) => new(true, null, null, id);

    public static ResultadoOperacion Fallo(string error, string? campo = null) => new(false, error, campo, null);

    public static ResultadoOperacion ConflictoConcurrencia() =>
        Fallo("Otro usuario modificó estos datos mientras los editabas. Volvé a abrir el formulario para ver los cambios y repetí la edición.");

    public static ResultadoOperacion NoEncontrado() =>
        Fallo("El registro no existe o fue eliminado.");
}
