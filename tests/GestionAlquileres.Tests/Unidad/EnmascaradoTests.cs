using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Models.Enums;
using GestionAlquileres.Web.Services.Personas;

namespace GestionAlquileres.Tests.Unidad;

public class EnmascaradoTests
{
    [Theory]
    [InlineData("11 5555-0101", "** ****-**01")]
    [InlineData("+54 9 11 5555-0101", "+** * ** ****-**01")]
    [InlineData("1", "1")]
    public void Telefono_deja_visibles_los_dos_ultimos_digitos(string telefono, string esperado)
    {
        Assert.Equal(esperado, Enmascarado.Telefono(telefono));
    }

    [Theory]
    [InlineData("maria.gonzalez@correo.test", "m***@correo.test")]
    [InlineData("sin-arroba", "***")]
    [InlineData("@correo.test", "***")]
    public void Email_deja_la_primera_letra_y_el_dominio(string email, string esperado)
    {
        Assert.Equal(esperado, Enmascarado.Email(email));
    }

    [Fact]
    public void Con_datos_completos_se_muestra_todo()
    {
        Assert.Equal("DNI 12.345.678", PersonaMapeo.MostrarDocumento(TipoDocumento.Dni, "12345678", completo: true));
        Assert.Equal("11 5555-0101", PersonaMapeo.MostrarTelefono("11 5555-0101", completo: true));
        Assert.Equal("ana@correo.test", PersonaMapeo.MostrarEmail("ana@correo.test", completo: true));
    }

    [Fact]
    public void Sin_datos_completos_nada_sale_entero()
    {
        var documento = PersonaMapeo.MostrarDocumento(TipoDocumento.Dni, "12345678", completo: false);
        var telefono = PersonaMapeo.MostrarTelefono("11 5555-0101", completo: false);
        var email = PersonaMapeo.MostrarEmail("ana.perez@correo.test", completo: false);

        Assert.Equal("DNI **.***.678", documento);
        Assert.DoesNotContain("12.345", documento);
        Assert.Equal("** ****-**01", telefono);
        Assert.Equal("a***@correo.test", email);
    }

    [Fact]
    public void Datos_vacios_quedan_vacios()
    {
        Assert.Null(PersonaMapeo.MostrarTelefono(null, completo: false));
        Assert.Null(PersonaMapeo.MostrarEmail(null, completo: false));
    }
}
