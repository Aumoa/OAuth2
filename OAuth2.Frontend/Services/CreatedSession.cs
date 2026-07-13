namespace OAuth2.Services;

public readonly record struct CreatedSession(string Id, DateTimeOffset ExpiresAt);
