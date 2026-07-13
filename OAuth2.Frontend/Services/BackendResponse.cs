using System.Net;

namespace OAuth2.Services;

public sealed record BackendResponse(
    HttpStatusCode StatusCode,
    string? Content,
    string? ContentType);

public sealed record BackendResponse<T>(
    HttpStatusCode StatusCode,
    T? Value,
    string? Content,
    string? ContentType);
