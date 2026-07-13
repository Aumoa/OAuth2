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
