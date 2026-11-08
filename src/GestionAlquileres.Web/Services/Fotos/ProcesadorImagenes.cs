using GestionAlquileres.Web.Infrastructure.Configuracion;
using SkiaSharp;

namespace GestionAlquileres.Web.Services.Fotos;

/// <summary>Foto lista para guardar: JPEG sin metadatos, en tamaño normal y miniatura.</summary>
public sealed record ImagenProcesada(byte[] Foto, byte[] Miniatura, int Ancho, int Alto);

/// <summary>
/// Valida y normaliza una imagen subida (con SkiaSharp, licencia MIT). No confía en la extensión ni en el tipo que
/// manda el navegador: identifica el formato por el contenido. Toda imagen aceptada se dibuja de nuevo y se vuelve
/// a codificar desde cero, lo que:
/// - quita los metadatos (EXIF con la ubicación GPS y el modelo del celular, IPTC, XMP): el codificador no los copia;
/// - corrige la orientación (fotos de celular "acostadas", según la orientación EXIF);
/// - limita el tamaño y genera la miniatura;
/// - descarta cualquier contenido extra que viniera escondido en el archivo original.
/// No usa base de datos ni disco: se testea directamente.
/// </summary>
public static class ProcesadorImagenes
{
    private const int CalidadFoto = 82;
    private const int CalidadMiniatura = 75;

    private static readonly SKSamplingOptions Muestreo = new(SKFilterMode.Linear, SKMipmapMode.Linear);

    /// <summary>Devuelve la imagen procesada, o el mensaje de error para mostrarle al usuario.</summary>
    public static async Task<(ImagenProcesada? Imagen, string? Error)> ProcesarAsync(
        Stream entrada,
        FotosOptions opciones,
        CancellationToken ct = default)
    {
        using var memoria = new MemoryStream();
        if (!await CopiarConLimiteAsync(entrada, memoria, opciones.TamanioMaximoBytes, ct))
        {
            return (null, $"pesa más de {opciones.TamanioMaximoMB} MB.");
        }

        if (memoria.Length == 0)
        {
            return (null, "está vacío.");
        }

        memoria.Position = 0;
        using var codec = SKCodec.Create(memoria); // lee solo el encabezado
        if (codec is null)
        {
            return (null, "no es una imagen válida.");
        }

        if (codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
        {
            return (null, "tiene un formato no permitido. Usá fotos JPG, PNG o WebP.");
        }

        // Antes de decodificar: una imagen enorme (o diseñada para agotar la memoria) se rechaza sin cargarla.
        if ((long)codec.Info.Width * codec.Info.Height > opciones.MegapixelesMaximos * 1_000_000L)
        {
            return (null, $"tiene una resolución demasiado grande (máximo {opciones.MegapixelesMaximos} megapíxeles).");
        }

        var orientacion = codec.EncodedOrigin;
        using var original = SKBitmap.Decode(codec);
        if (original is null)
        {
            return (null, "está dañada o no se pudo leer.");
        }

        using var orientada = Orientar(original, orientacion);
        using var foto = Achicar(orientada, opciones.LadoMaximo);
        using var miniatura = Achicar(foto, opciones.LadoMiniatura);

        return (new ImagenProcesada(
            CodificarJpeg(foto, CalidadFoto),
            CodificarJpeg(miniatura, CalidadMiniatura),
            foto.Width,
            foto.Height), null);
    }

    /// <summary>Aplica la orientación EXIF dibujando la imagen girada o espejada (el resultado ya no la necesita).</summary>
    private static SKBitmap Orientar(SKBitmap origen, SKEncodedOrigin orientacion)
    {
        if (orientacion is SKEncodedOrigin.TopLeft or SKEncodedOrigin.Default)
        {
            return origen.Copy();
        }

        float w = origen.Width, h = origen.Height;
        var intercambia = orientacion is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var destino = new SKBitmap(intercambia ? origen.Height : origen.Width, intercambia ? origen.Width : origen.Height);

        using var lienzo = new SKCanvas(destino);
        switch (orientacion)
        {
            case SKEncodedOrigin.TopRight: // espejo horizontal
                lienzo.Translate(w, 0);
                lienzo.Scale(-1, 1);
                break;
            case SKEncodedOrigin.BottomRight: // 180°
                lienzo.Translate(w, h);
                lienzo.RotateDegrees(180);
                break;
            case SKEncodedOrigin.BottomLeft: // espejo vertical
                lienzo.Translate(0, h);
                lienzo.Scale(1, -1);
                break;
            case SKEncodedOrigin.LeftTop: // transpuesta
                lienzo.Scale(1, -1);
                lienzo.RotateDegrees(-90);
                break;
            case SKEncodedOrigin.RightTop: // 90° a la derecha (la más común en fotos de celular en vertical)
                lienzo.Translate(h, 0);
                lienzo.RotateDegrees(90);
                break;
            case SKEncodedOrigin.RightBottom: // transversa
                lienzo.Translate(h, w);
                lienzo.Scale(1, -1);
                lienzo.RotateDegrees(90);
                break;
            case SKEncodedOrigin.LeftBottom: // 90° a la izquierda
                lienzo.Translate(0, w);
                lienzo.RotateDegrees(-90);
                break;
        }

        lienzo.DrawBitmap(origen, 0, 0, Muestreo, null);
        return destino;
    }

    /// <summary>Reduce para que el lado mayor no supere el máximo (nunca agranda). Siempre devuelve una copia nueva.</summary>
    private static SKBitmap Achicar(SKBitmap origen, int ladoMaximo)
    {
        var escala = Math.Min(1.0, (double)ladoMaximo / Math.Max(origen.Width, origen.Height));
        if (escala >= 1.0)
        {
            return origen.Copy();
        }

        var ancho = Math.Max(1, (int)Math.Round(origen.Width * escala));
        var alto = Math.Max(1, (int)Math.Round(origen.Height * escala));
        return origen.Resize(new SKImageInfo(ancho, alto, SKColorType.Rgba8888, SKAlphaType.Premul), Muestreo);
    }

    /// <summary>JPEG sobre fondo blanco (JPEG no tiene transparencia: los PNG transparentes quedarían negros).</summary>
    private static byte[] CodificarJpeg(SKBitmap imagen, int calidad)
    {
        using var plana = new SKBitmap(imagen.Width, imagen.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using (var lienzo = new SKCanvas(plana))
        {
            lienzo.Clear(SKColors.White);
            lienzo.DrawBitmap(imagen, 0, 0, Muestreo, null);
        }

        using var datos = SKImage.FromBitmap(plana).Encode(SKEncodedImageFormat.Jpeg, calidad);
        return datos.ToArray();
    }

    /// <summary>Copia sin superar el límite (no se confía en el tamaño declarado). False si lo supera.</summary>
    private static async Task<bool> CopiarConLimiteAsync(Stream entrada, Stream salida, long limite, CancellationToken ct)
    {
        var buffer = new byte[81920];
        long total = 0;
        int leidos;
        while ((leidos = await entrada.ReadAsync(buffer, ct)) > 0)
        {
            total += leidos;
            if (total > limite)
            {
                return false;
            }

            await salida.WriteAsync(buffer.AsMemory(0, leidos), ct);
        }

        return true;
    }
}
