using System.Security.Cryptography;
using System.Text;
using OAuth2.Data;
using OAuth2.Services;
using SkiaSharp;

namespace OAuth2.Backend.Tests.Services;

public sealed class ProfileImageProcessorTests
{
    [Fact]
    public async Task ProcessAsync_ResizesAndReencodesWithoutTrailingPayload()
    {
        var payload = Encoding.UTF8.GetBytes("<script>malicious-payload</script>");
        var source = CreateImage(SKEncodedImageFormat.Png, 1024, 512);
        var input = source.Concat(payload).ToArray();
        var processor = new ProfileImageProcessor();

        var result = await processor.ProcessAsync(new MemoryStream(input));

        Assert.Equal(256, result.Width);
        Assert.Equal(128, result.Height);
        Assert.InRange(result.Data.Length, 1, ProfileImagePolicy.MaxStoredBytes);
        Assert.Equal(SHA256.HashData(result.Data), result.Hash);
        Assert.Equal(-1, result.Data.AsSpan().IndexOf(payload));

        using var normalizedData = SKData.CreateCopy(result.Data);
        using var codec = SKCodec.Create(normalizedData);
        Assert.NotNull(codec);
        Assert.Equal(SKEncodedImageFormat.Webp, codec.EncodedFormat);
        Assert.Equal(256, codec.Info.Width);
        Assert.Equal(128, codec.Info.Height);
        Assert.InRange(codec.FrameCount, 0, 1);
    }

    [Fact]
    public async Task ProcessAsync_RejectsInvalidData()
    {
        var processor = new ProfileImageProcessor();

        await Assert.ThrowsAsync<ProfileImageProcessingException>(() =>
            processor.ProcessAsync(new MemoryStream([1, 2, 3])));
    }

    [Fact]
    public async Task ProcessAsync_RejectsOversizedEncodedInput()
    {
        var processor = new ProfileImageProcessor();
        var input = new byte[ProfileImagePolicy.MaxUploadBytes + 1];

        await Assert.ThrowsAsync<ProfileImageProcessingException>(() =>
            processor.ProcessAsync(new MemoryStream(input)));
    }

    [Fact]
    public async Task ProcessAsync_RejectsOversizedDecodedDimensions()
    {
        var processor = new ProfileImageProcessor();
        var source = CreateImage(
            SKEncodedImageFormat.Png,
            ProfileImagePolicy.MaxSourceDimension + 1,
            1);

        await Assert.ThrowsAsync<ProfileImageProcessingException>(() =>
            processor.ProcessAsync(new MemoryStream(source)));
    }

    private static byte[] CreateImage(
        SKEncodedImageFormat format,
        int width,
        int height)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        surface.Canvas.Clear(new SKColor(45, 180, 120, 255));
        using var image = surface.Snapshot();
        using var data = image.Encode(format, 95);
        return data.ToArray();
    }
}
