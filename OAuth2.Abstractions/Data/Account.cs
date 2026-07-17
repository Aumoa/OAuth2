namespace OAuth2.Data;

public record Account
{
    public string? Id { get; set; }

    public string? Password { get; set; }

    public string? Sub { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? VerifyCode { get; set; }

    public DateTime? VerifyCodeExpiresAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? ProfileImageVersion { get; set; }
}
