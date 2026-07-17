namespace OAuth2.DataTransfer;

public sealed record OidcAuthorizationValidation
{
    public bool IsValid { get; init; }

    public bool CanRedirect { get; init; }

    public string? NormalizedScope { get; init; }

    public string? ClientName { get; init; }

    public string? Error { get; init; }
}
