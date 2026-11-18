using Avalonia.Media.Imaging;
using SkiaSharp;

namespace DigitalWorkstation.MailReader;

internal static class MailImageDecoder
{
    internal static bool Supports(string mediaType) => mediaType.ToLowerInvariant() is
        "image/png" or "image/jpeg" or "image/gif" or "image/bmp" or "image/webp";

    public static Bitmap Decode(byte[] data)
    {
        if (data.Length > 20 * 1024 * 1024) throw new InvalidDataException("Image exceeds the encoded size limit.");
        using var metadataStream = new MemoryStream(data, false);
        using var codec = SKCodec.Create(metadataStream) ?? throw new InvalidDataException("Unsupported image.");
        var width = codec.Info.Width;
        var height = codec.Info.Height;
        if (width <= 0 || height <= 0 || (long)width * height > 40_000_000)
            throw new InvalidDataException("Image exceeds the source pixel limit.");
        var scale = Math.Min(1d, Math.Min(1200d / width, 600d / height));
        var targetWidth = Math.Max(1, (int)(width * scale));
        var targetHeight = Math.Max(1, (int)(height * scale));
        // Clamp the minor axis to one pixel: Avalonia's proportional helper can round it
        // to zero for extreme aspect ratios. Source allocation is bounded above as well.
        using var source = SKBitmap.Decode(data) ?? throw new InvalidDataException("Invalid image.");
        using var resized = source.Resize(new SKImageInfo(targetWidth, targetHeight), SKFilterQuality.Medium)
            ?? throw new InvalidDataException("Unable to resize image.");
        using var encoded = resized.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = encoded.AsStream();
        return new Bitmap(stream);
    }
}
