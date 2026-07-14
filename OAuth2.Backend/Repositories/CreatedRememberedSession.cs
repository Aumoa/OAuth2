namespace OAuth2.Repositories;

public readonly record struct CreatedRememberedSession(
    string Token,
    DateTimeOffset ExpiresAt);
