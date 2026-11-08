namespace GestionAlquileres.Web.Infrastructure;

/// <summary>
/// Versiones parcialmente ocultas de datos de contacto, para roles sin acceso a datos personales completos.
/// (El documento se enmascara con <see cref="Documentos.Enmascarar"/>.)
/// </summary>
public static class Enmascarado
{
    /// <summary>Oculta todos los dígitos salvo los dos últimos, conservando el formato: "11 5555-0101" → "** ****-**01".</summary>
    public static string Telefono(string telefono)
    {
        var caracteres = telefono.ToCharArray();
        var visibles = 0;
        for (var i = caracteres.Length - 1; i >= 0; i--)
        {
            if (!char.IsDigit(caracteres[i]))
            {
                continue;
            }

            if (visibles >= 2)
            {
                caracteres[i] = '*';
            }

            visibles++;
        }

        return new string(caracteres);
    }

    /// <summary>Deja la primera letra y el dominio: "maria.gonzalez@correo.test" → "m***@correo.test".</summary>
    public static string Email(string email)
    {
        var arroba = email.IndexOf('@');
        return arroba <= 0 ? "***" : $"{email[0]}***{email[arroba..]}";
    }
}
