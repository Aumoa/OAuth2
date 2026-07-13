using System.ComponentModel.DataAnnotations;

namespace OAuth2.Options;

public record SESOptions
{
    [Required, EmailAddress]
    public string SenderAddress { get; init; } = string.Empty;

    public string SenderName { get; init; } = "OAuth2";

    public required string AccessKey { get; init; }

    public required string SecretKey { get; init; }

    [Required]
    public string Region { get; init; } = "ap-northeast-2";

    [Required, Url]
    public string VerificationUrl { get; init; } = string.Empty;

    [Range(1, 24 * 60)]
    public int VerificationCodeValidMinutes { get; init; } = 30;
}
