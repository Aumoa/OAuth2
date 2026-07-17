using System.Text.Json.Serialization;

namespace OAuth2.DataTransfer;

public static class LoginStates
{
    public const string Authenticated = "authenticated";

    public const string EmailVerificationRequired = "emailVerificationRequired";
}

public sealed record LoginResponse
{
    public required string State { get; init; }

    public string? Sub { get; init; }

    public string? RedirectUri { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GrantedUserInfo? SessionGrant { get; init; }
}
