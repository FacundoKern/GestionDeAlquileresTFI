using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Models.Base;
using GestionAlquileres.Web.Models.Enums;
using GestionAlquileres.Web.ViewModels.Personas;

namespace GestionAlquileres.Web.Services.Personas;

/// <summary>Conversión entre la entidad <see cref="Persona"/> y sus ViewModels (común a las tres entidades).</summary>
public static class PersonaMapeo
{
    /// <summary>Copia solo los campos editables, normalizados.</summary>
    public static void CopiarA(PersonaFormViewModel modelo, Persona persona)
    {
        persona.TipoPersona = modelo.TipoPersona;
        persona.Nombre = modelo.Nombre.Trim();
        persona.Apellido = modelo.TipoPersona == TipoPersona.Juridica ? null : Limpiar(modelo.Apellido);
        persona.TipoDocumento = modelo.TipoDocumento;
        persona.NumeroDocumento = Documentos.Normalizar(modelo.NumeroDocumento);
        persona.Email = Limpiar(modelo.Email);
        persona.Telefono = Limpiar(modelo.Telefono);
        persona.Domicilio = Limpiar(modelo.Domicilio);
        persona.Notas = Limpiar(modelo.Notas);
    }

    public static T AFormulario<T>(Persona persona) where T : PersonaFormViewModel, new() => new()
    {
        Id = persona.Id,
        TipoPersona = persona.TipoPersona,
        Nombre = persona.Nombre,
        Apellido = persona.Apellido,
        TipoDocumento = persona.TipoDocumento,
        NumeroDocumento = Documentos.Formatear(persona.TipoDocumento, persona.NumeroDocumento),
        Email = persona.Email,
        Telefono = persona.Telefono,
        Domicilio = persona.Domicilio,
        Notas = persona.Notas,
        RowVersion = Convert.ToBase64String(persona.RowVersion),
    };

    public static PersonaDatosViewModel ADatos(Persona persona, bool datosCompletos) => new(
        persona.NombreCompleto,
        persona.TipoPersona,
        MostrarDocumento(persona.TipoDocumento, persona.NumeroDocumento, datosCompletos),
        !datosCompletos,
        MostrarEmail(persona.Email, datosCompletos),
        MostrarTelefono(persona.Telefono, datosCompletos),
        persona.Domicilio,
        persona.Notas,
        persona.CreadoPor,
        persona.CreadoEnUtc,
        persona.ModificadoPor,
        persona.ModificadoEnUtc);

    /// <summary>
    /// Documento para mostrar: completo para Administrador/Operador, enmascarado para Consulta.
    /// El enmascarado se hace en el servidor: el número completo nunca llega al navegador.
    /// </summary>
    public static string MostrarDocumento(TipoDocumento tipo, string numero, bool completo) =>
        $"{tipo.Nombre()} {(completo ? Documentos.Formatear(tipo, numero) : Documentos.Enmascarar(tipo, numero))}";

    /// <summary>Teléfono completo o parcialmente oculto (también resuelto en el servidor).</summary>
    public static string? MostrarTelefono(string? telefono, bool completo) =>
        string.IsNullOrWhiteSpace(telefono) || completo ? telefono : Enmascarado.Telefono(telefono);

    /// <summary>Email completo o parcialmente oculto (también resuelto en el servidor).</summary>
    public static string? MostrarEmail(string? email, bool completo) =>
        string.IsNullOrWhiteSpace(email) || completo ? email : Enmascarado.Email(email);

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
