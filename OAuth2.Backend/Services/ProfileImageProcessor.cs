using System.Security.Cryptography;
using OAuth2.Data;
using OAuth2.Repositories;
using SkiaSharp;

namespace OAuth2.Services;

public sealed class ProfileImageProcessor
{
    public async Task<ProcessedProfileImage> ProcessAsync(
        Stream source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        var encodedBytes = await ReadInputAsync(source, cancellationToken);
        using var encodedData = SKData.CreateCopy(encodedBytes);
        using var codec = SKCodec.Create(encodedData)
            ?? throw InvalidImage();

        if (codec.EncodedFormat is not (
                SKEncodedImageFormat.Jpeg
                or SKEncodedImageFormat.Png
                or SKEncodedImageFormat.Webp)
            || codec.FrameCount > 1)
        {
            throw new ProfileImageProcessingException(
                "Only single-frame JPEG, PNG, and WebP images are supported.");
        }

        var sourceInfo = codec.Info;
        if (sourceInfo.Width <= 0
            || sourceInfo.Height <= 0
            || sourceInfo.Width > ProfileImagePolicy.MaxSourceDimension
            || sourceInfo.Height > ProfileImagePolicy.MaxSourceDimension
            || (long)sourceInfo.Width * sourceInfo.Height > ProfileImagePolicy.MaxSourcePixelCount)
        {
            throw new ProfileImageProcessingException("The decoded image dimensions are too large.");
        }

        using var colorSpace = SKColorSpace.CreateSrgb();
        var decodeInfo = new SKImageInfo(
            sourceInfo.Width,
            sourceInfo.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul,
            colorSpace);
        using var bitmap = new SKBitmap(decodeInfo);
        var decodeResult = codec.GetPixels(decodeInfo, bitmap.GetPixels());
        if (decodeResult != SKCodecResult.Success)
        {
            throw InvalidImage();
        }

        var scale = Math.Min(
            1D,
            ProfileImagePolicy.MaxStoredDimension / (double)Math.Max(sourceInfo.Width, sourceInfo.Height));
        var width = Math.Max(1, (int)Math.Round(sourceInfo.Width * scale));
        var height = Math.Max(1, (int)Math.Round(sourceInfo.Height * scale));
        var outputInfo = new SKImageInfo(
            width,
            height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul,
            colorSpace);
        using var surface = SKSurface.Create(outputInfo)
            ?? throw new ProfileImageProcessingException("The image could not be resized.");

        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.DrawBitmap(
            bitmap,
            new SKRect(0, 0, sourceInfo.Width, sourceInfo.Height),
            new SKRect(0, 0, width, height),
            new SKSamplingOptions(SKCubicResampler.Mitchell));
        surface.Canvas.Flush();

        using var normalizedImage = surface.Snapshot();
        using var normalizedData = normalizedImage.Encode(SKEncodedImageFormat.Webp, 88)
            ?? throw new ProfileImageProcessingException("The image could not be encoded.");
        var data = normalizedData.ToArray();
        if (data.Length == 0 || data.Length > ProfileImagePolicy.MaxStoredBytes)
        {
            throw new ProfileImageProcessingException("The processed image is too large.");
        }

        return new ProcessedProfileImage
        {
            Data = data,
            Width = width,
            Height = height,
            Hash = SHA256.HashData(data)
        };
    }

    private static async Task<byte[]> ReadInputAsync(
        Stream source,
        CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[64 * 1024];
        while (true)
        {
            var read = await source.ReadAsync(chunk, cancellationToken);
            if (read == 0)
            {
                break;
            }

            if (buffer.Length + read > ProfileImagePolicy.MaxUploadBytes)
            {
                throw new ProfileImageProcessingException("The uploaded image is too large.");
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        if (buffer.Length == 0)
        {
            throw InvalidImage();
        }

        return buffer.ToArray();
    }

    private static ProfileImageProcessingException InvalidImage() =>
        new("The uploaded data is not a valid image.");
}
