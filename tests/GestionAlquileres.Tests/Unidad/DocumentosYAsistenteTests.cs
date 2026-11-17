using System.Text;
using GestionAlquileres.Web.Services.Asistente;
using GestionAlquileres.Web.Services.Contratos;

namespace GestionAlquileres.Tests.Unidad;

public class ValidadorPdfTests
{
    private const long Maximo = 1024;

    [Fact]
    public void Un_pdf_se_reconoce_por_su_contenido()
    {
        Assert.Null(ValidadorPdf.Validar(Encoding.ASCII.GetBytes("%PDF-1.7\n..."), Maximo));
    }

    [Theory]
    [InlineData("MZ\u0090\u0000 ejecutable renombrado a .pdf")]
    [InlineData("<html><script>alert(1)</script></html>")]
    [InlineData(" %PDF-1.7 con un espacio adelante")]
    public void Lo_que_no_empieza_con_la_firma_de_pdf_se_rechaza(string contenido)
    {
        Assert.Equal("El archivo no es un PDF. Solo se aceptan documentos PDF.", ValidadorPdf.Validar(Encoding.Latin1.GetBytes(contenido), Maximo));
    }

    [Fact]
    public void Se_rechazan_los_archivos_vacios_y_los_que_superan_el_maximo()
    {
        Assert.Equal("El archivo está vacío.", ValidadorPdf.Validar([], Maximo));

        var grande = new byte[Maximo + 1];
        Encoding.ASCII.GetBytes("%PDF-").CopyTo(grande, 0);
        Assert.NotNull(ValidadorPdf.Validar(grande, Maximo));
    }
}

public class FiltroDatosPersonalesTests
{
    [Theory]
    [InlineData("¿Cómo cargo un inquilino nuevo?")]
    [InlineData("¿Qué puede hacer el rol Consulta?")]
    [InlineData("¿Cómo adjunto el contrato firmado de 2026?")]
    [InlineData("Un contrato de $ 1.850.000 en pesos, ¿cómo lo ajusto?")]
    [InlineData("¿Se ajusta cada 3 o 6 meses?")]
    public void Las_preguntas_sobre_el_uso_del_sistema_pasan(string pregunta)
    {
        Assert.Null(FiltroDatosPersonales.Detectar(pregunta));
    }

    [Theory]
    [InlineData("¿Qué contratos tiene el DNI 20.345.678?", "un número de documento, CUIT o teléfono")]
    [InlineData("Buscame el CUIT 30-70987654-3", "un número de documento, CUIT o teléfono")]
    [InlineData("Llamá al 11 5555-0101", "un número de documento, CUIT o teléfono")]
    [InlineData("el inquilino 20345678 no paga", "un número de documento, CUIT o teléfono")]
    [InlineData("¿Le escribo a maria.gonzalez@correo.test?", "un email")]
    public void Los_datos_personales_se_detectan_antes_de_enviar(string pregunta, string esperado)
    {
        Assert.Equal(esperado, FiltroDatosPersonales.Detectar(pregunta));
    }
}
