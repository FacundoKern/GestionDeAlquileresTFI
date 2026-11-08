namespace GestionAlquileres.Web.Models.Base;

/// <summary>
/// Borrado lógico: al eliminar, el DbContext marca <see cref="Eliminado"/> en lugar de borrar la fila,
/// y un filtro global oculta las filas eliminadas en todas las consultas.
/// </summary>
public interface IBorradoLogico
{
    bool Eliminado { get; set; }

    DateTime? EliminadoEnUtc { get; set; }

    string? EliminadoPor { get; set; }
}

/// <summary>Registro inmutable: solo se insertan filas; modificar o eliminar lanza una excepción.</summary>
public interface ISoloAgregar;

/// <summary>Entidad que nunca se elimina, ni siquiera lógicamente (p. ej. recibos: se anulan).</summary>
public interface INoEliminable;

/// <summary>
/// Entidad que, una vez guardada, solo admite cambios en las propiedades marcadas con
/// <see cref="ModificableTrasEmisionAttribute"/> (p. ej. un recibo emitido solo puede anularse).
/// </summary>
public interface IInmutableTrasEmision;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ModificableTrasEmisionAttribute : Attribute;

/// <summary>Excluye una clase o propiedad del registro de auditoría (AuditLog).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public sealed class NoAuditarAttribute : Attribute;
