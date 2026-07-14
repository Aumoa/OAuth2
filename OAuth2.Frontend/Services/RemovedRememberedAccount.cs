namespace OAuth2.Services;

public readonly record struct RemovedRememberedAccount(
    bool Found,
    string? Token,
    bool HasRemainingAccounts);
