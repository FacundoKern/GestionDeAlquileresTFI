using System.Text;
using GestionAlquileres.Web.Infrastructure.Configuracion;
using GestionAlquileres.Web.Services.Fotos;
using SkiaSharp;

namespace GestionAlquileres.Tests.Unidad;

public class ProcesadorImagenesTests
{
    private static readonly FotosOptions Opciones = new() { LadoMaximo = 1600, LadoMiniatura = 400, TamanioMaximoMB = 10, MegapixelesMaximos = 50 };

    /// <summary>Imagen de prueba con un cuadrado rojo arriba a la izquierda (para verificar la orientación).</summary>
    public static byte[] CrearImagen(int ancho, int alto, SKEncodedImageFormat formato = SKEncodedImageFormat.Jpeg, bool transparente = false)
    {
        using var bitmap = new SKBitmap(ancho, alto, SKColorType.Rgba8888, transparente ? SKAlphaType.Premul : SKAlphaType.Opaque);
        using (var lienzo = new SKCanvas(bitmap))
        {
            lienzo.Clear(transparente ? SKColors.Transparent : SKColors.SteelBlue);
            using var rojo = new SKPaint { Color = SKColors.Red };
            lienzo.DrawRect(0, 0, ancho / 4f, alto / 4f, rojo);
        }

        using var imagen = SKImage.FromBitmap(bitmap);
        using var datos = imagen.Encode(formato, 90);
        return datos.ToArray();
    }

    /// <summary>Inserta un bloque EXIF con la orientación indicada (6 = girada 90° a la derecha, típica de celular).</summary>
    private static byte[] ConOrientacionExif(byte[] jpeg, byte orientacion)
    {
        byte[] app1 =
        [
            0xFF, 0xE1, 0x00, 0x22, // APP1, largo 34
            0x45, 0x78, 0x69, 0x66, 0x00, 0x00, // "Exif\0\0"
            0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00, // TIFF little endian, primer IFD en 8
            0x01, 0x00, // 1 entrada
            0x12, 0x01, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, orientacion, 0x00, 0x00, 0x00, // Orientation (0x0112)
            0x00, 0x00, 0x00, 0x00, // sin más IFD
        ];
        return [.. jpeg[..2], .. app1, .. jpeg[2..]];
    }

    private static async Task<(ImagenProcesada? Imagen, string? Error)> Procesar(byte[] bytes, FotosOptions? opciones = null) =>
        await ProcesadorImagenes.ProcesarAsync(new MemoryStream(bytes), opciones ?? Opciones);

    private static SKBitmap Leer(byte[] jpeg) => SKBitmap.Decode(jpeg);

    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg)]
    [InlineData(SKEncodedImageFormat.Png)]
    [InlineData(SKEncodedImageFormat.Webp)]
    public async Task Acepta_jpg_png_y_webp_y_siempre_guarda_jpeg(SKEncodedImageFormat formato)
    {
        var (imagen, error) = await Procesar(CrearImagen(800, 600, formato));

        Assert.Null(error);
        Assert.NotNull(imagen);
        using var codec = SKCodec.Create(new MemoryStream(imagen.Foto));
        Assert.Equal(SKEncodedImageFormat.Jpeg, codec.EncodedFormat);
        Assert.Equal((800, 600), (imagen.Ancho, imagen.Alto)); // no se agranda ni se achica si ya es chica
    }

    [Fact]
    public async Task Achica_la_foto_y_genera_la_miniatura_respetando_la_proporcion()
    {
        var (imagen, _) = await Procesar(CrearImagen(3000, 2000));

        Assert.Equal((1600, 1067), (imagen!.Ancho, imagen.Alto));
        using var miniatura = Leer(imagen.Miniatura);
        Assert.Equal((400, 267), (miniatura.Width, miniatura.Height));
    }

    [Fact]
    public async Task Corrige_la_orientacion_y_quita_los_metadatos_exif()
    {
        var conExif = ConOrientacionExif(CrearImagen(400, 200), orientacion: 6);

        var (imagen, error) = await Procesar(conExif);

        Assert.Null(error);
        Assert.Equal((200, 400), (imagen!.Ancho, imagen.Alto)); // girada: ahora es vertical
        Assert.DoesNotContain("Exif", Encoding.ASCII.GetString(imagen.Foto));
        Assert.DoesNotContain("Exif", Encoding.ASCII.GetString(imagen.Miniatura));

        // El cuadrado rojo (arriba a la izquierda en el original) queda arriba a la derecha al girar 90° a la derecha.
        using var resultado = Leer(imagen.Foto);
        Assert.True(resultado.GetPixel(190, 10).Red > 200);
        Assert.True(resultado.GetPixel(10, 10).Red < 100);
    }

    [Fact]
    public async Task Las_transparencias_quedan_sobre_blanco()
    {
        var (imagen, _) = await Procesar(CrearImagen(200, 200, SKEncodedImageFormat.Png, transparente: true));

        using var resultado = Leer(imagen!.Foto);
        var centro = resultado.GetPixel(150, 150);
        Assert.True(centro is { Red: > 240, Green: > 240, Blue: > 240 }, $"Se esperaba blanco y es {centro}");
    }

    [Fact]
    public async Task Rechaza_un_archivo_que_no_es_imagen_aunque_se_llame_jpg()
    {
        var (imagen, error) = await Procesar(Encoding.UTF8.GetBytes("<script>alert('hola')</script>"));

        Assert.Null(imagen);
        Assert.Equal("no es una imagen válida.", error);
    }

    [Fact]
    public async Task Rechaza_formatos_no_permitidos()
    {
        var gif = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");

        var (imagen, error) = await Procesar(gif);

        Assert.Null(imagen);
        Assert.Contains("formato no permitido", error);
    }

    [Fact]
    public async Task Rechaza_archivos_vacios_y_demasiado_pesados()
    {
        Assert.Equal("está vacío.", (await Procesar([])).Error);

        var opciones = new FotosOptions { TamanioMaximoMB = 1 };
        var pesado = new byte[(int)(1.5 * 1024 * 1024)];
        Assert.Equal("pesa más de 1 MB.", (await Procesar(pesado, opciones)).Error);
    }

    [Fact]
    public async Task Rechaza_resoluciones_demasiado_grandes_sin_decodificarlas()
    {
        var opciones = new FotosOptions { MegapixelesMaximos = 1 };

        var (imagen, error) = await Procesar(CrearImagen(1200, 1000), opciones);

        Assert.Null(imagen);
        Assert.Contains("resolución demasiado grande", error);
    }
}
