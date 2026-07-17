namespace OAuth2.Repositories;

public interface IRefreshTokens
{
    const int MaxTokenLength = 256;

    const int MaxScopeLength = 512;

    Task<string> CreateAsync(
        string accountId,
        string clientId,
        string scope,
        long authTime,
        CancellationToken cancellationToken = default);

    Task<RefreshTokenRotationResult> RotateAsync(
        string clientId,
        string token,
        string? requestedScope,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        string clientId,
        string token,
        CancellationToken cancellationToken = default);
}
