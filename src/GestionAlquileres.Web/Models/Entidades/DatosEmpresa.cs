using GestionAlquileres.Web.Models.Base;

namespace GestionAlquileres.Web.Models.Entidades;

/// <summary>
/// Datos de la inmobiliaria (una sola fila). Los edita el Administrador y se usan en el pie de página,
/// en el inicio y en el encabezado de los PDFs (recibos, avisos). Los cambios quedan auditados.
/// </summary>
public class DatosEmpresa : EntidadAuditable
{
    public string NombreComercial { get; set; } = string.Empty;

    public string? RazonSocial { get; set; }

    /// <summary>Normalizado: solo dígitos.</summary>
    public string? Cuit { get; set; }

    /// <summary>Matrícula del corredor inmobiliario (p. ej. CUCICBA o colegio provincial).</summary>
    public string? MatriculaCorredor { get; set; }

    public string? Direccion { get; set; }

    public string? Localidad { get; set; }

    public string? Provincia { get; set; }

    public string? CodigoPostal { get; set; }

    public string? Telefono { get; set; }

    public string? Whatsapp { get; set; }

    public string? Email { get; set; }

    public string? SitioWeb { get; set; }

    public string? HorarioAtencion { get; set; }

    /// <summary>Breve descripción del negocio para el inicio.</summary>
    public string? Descripcion { get; set; }

    public string? SoporteNombre { get; set; }

    public string? SoporteTelefono { get; set; }

    public string? SoporteEmail { get; set; }
}
