using System.Net;

namespace OAuth2.Services;

public sealed record BackendBinaryResponse(
    HttpStatusCode StatusCode,
    byte[]? Content,
    string? ContentType,
    string? EntityTag,
    string? CacheControl);
