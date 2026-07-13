namespace OAuth2.Options;

public sealed record RedisOptions
{
    public required string ConnectionString { get; init; }

    public int DbIndex { get; init; }
}
