namespace OAuth2.Data;

public static class ProfileImagePolicy
{
    public const long MaxUploadBytes = 4 * 1024 * 1024;

    public const int MaxSourceDimension = 8192;

    public const long MaxSourcePixelCount = 16 * 1024 * 1024;

    public const int MaxStoredDimension = 256;

    public const int MaxStoredBytes = 1024 * 1024;

    public const string StoredContentType = "image/webp";
}
