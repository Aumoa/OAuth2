namespace OAuth2.Options;

public sealed record MigrationOptions
{
    public bool RunOnStartup { get; init; }
}
