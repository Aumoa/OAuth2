namespace OAuth2.Options;

public record PasswordHasherOptions
{
    public int Iterations { get; init; } = 100000;
}
