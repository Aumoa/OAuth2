namespace OAuth2.Options;

public record MySqlOptions
{
    public required string Server { get; init; }

    public int Port { get; init; } = 3306;

    public required string Database { get; init; }

    public required string User { get; init; }

    public required string Password { get; init; }

    public string ConnectionString => $"Server={Server};Port={Port};Database={Database};User={User};Password={Password}";
}
