using GestionAlquileres.Web.Infrastructure;
using GestionAlquileres.Web.Models.Enums;

namespace GestionAlquileres.Tests.Unidad;

public class DocumentosTests
{
    [Theory]
    [InlineData("20-12345678-6")]
    [InlineData("20123456786")]
    [InlineData(" 20.12345678 6 ")]
    [InlineData("30-71555666-5")]
    [InlineData("00000000000")] // resto 0: el dígito verificador es 0
    [InlineData("00000000069")] // resto 1: el dígito verificador es 9
    public void Cuit_valido(string cuit)
    {
        Assert.True(Documentos.EsCuitValido(cuit));
    }

    [Theory]
    [InlineData("20-12345678-5")] // dígito verificador incorrecto
    [InlineData("2012345678")] // 10 dígitos
    [InlineData("201234567861")] // 12 dígitos
    [InlineData("20-1234567A-6")]
    [InlineData("")]
    [InlineData(null)]
    public void Cuit_invalido(string? cuit)
    {
        Assert.False(Documentos.EsCuitValido(cuit));
    }

    [Theory]
    [InlineData("2012345678", 6)]
    [InlineData("0000000000", 0)]
    [InlineData("0000000006", 9)]
    public void Digito_verificador_del_cuit(string prefijo, int esperado)
    {
        Assert.Equal(esperado, Documentos.CalcularDigitoVerificadorCuit(prefijo));
    }

    [Theory]
    [InlineData(TipoDocumento.Dni, "12.345.678", true)]
    [InlineData(TipoDocumento.Dni, "1234567", true)]
    [InlineData(TipoDocumento.Dni, "123456", false)]
    [InlineData(TipoDocumento.Dni, "123456789", false)]
    [InlineData(TipoDocumento.Dni, "12a45678", false)]
    [InlineData(TipoDocumento.Cuil, "20-12345678-6", true)]
    [InlineData(TipoDocumento.Cuil, "20-12345678-0", false)]
    [InlineData(TipoDocumento.Pasaporte, "AAB123456", true)]
    [InlineData(TipoDocumento.Pasaporte, "AB12", false)]
    [InlineData(TipoDocumento.Dni, "", false)]
    public void Validar_por_tipo_de_documento(TipoDocumento tipo, string numero, bool esValido)
    {
        Assert.Equal(esValido, Documentos.Validar(tipo, numero) is null);
    }

    [Fact]
    public void Normalizar_quita_separadores_y_pasa_a_mayusculas()
    {
        Assert.Equal("20123456786", Documentos.Normalizar(" 20-12.345 678-6 "));
        Assert.Equal("AAB123", Documentos.Normalizar("aab-123"));
        Assert.Equal(string.Empty, Documentos.Normalizar(null));
    }

    [Theory]
    [InlineData(TipoDocumento.Dni, "12345678", "12.345.678")]
    [InlineData(TipoDocumento.Dni, "1234567", "1.234.567")]
    [InlineData(TipoDocumento.Cuit, "20123456786", "20-12345678-6")]
    [InlineData(TipoDocumento.Pasaporte, "aab123456", "AAB123456")]
    public void Formatear(TipoDocumento tipo, string numero, string esperado)
    {
        Assert.Equal(esperado, Documentos.Formatear(tipo, numero));
    }

    [Theory]
    [InlineData(TipoDocumento.Dni, "12345678", "**.***.678")]
    [InlineData(TipoDocumento.Cuit, "20123456786", "**-******78-6")]
    [InlineData(TipoDocumento.Pasaporte, "AAB123456", "******456")]
    public void Enmascarar_deja_visibles_solo_los_ultimos_tres(TipoDocumento tipo, string numero, string esperado)
    {
        Assert.Equal(esperado, Documentos.Enmascarar(tipo, numero));
    }
}
