namespace OAuth2.Repositories;

public readonly record struct AuthorizationCodeBody(
    string AccountId,
    string ClientId,
    string Scope,
    string RedirectUri,
    string? Nonce,
    string? CodeChallenge,
    string? CodeChallengeMethod,
    long AuthTime);

public interface IAuthorizationCodes
{
    ValueTask<string> PushAsync(
        AuthorizationCodeBody body,
        CancellationToken cancellationToken = default);

    ValueTask<AuthorizationCodeBody?> PopAsync(
        string code,
        CancellationToken cancellationToken = default);
}
