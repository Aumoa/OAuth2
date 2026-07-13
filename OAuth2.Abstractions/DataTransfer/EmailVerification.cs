using System.Diagnostics.CodeAnalysis;

namespace OAuth2.DataTransfer;

public sealed record EmailVerificationChallenge
{
    public required string Sub { get; init; }
}

public sealed record EmailVerificationForm
{
    public required string Sub { get; init; }

    public required string Code { get; init; }

    public bool Verify([NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrWhiteSpace(Sub))
        {
            error = "body.sub is missing";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Code))
        {
            error = "body.code is missing";
            return false;
        }

        error = null;
        return true;
    }
}

public sealed record EmailVerificationResendForm
{
    public required string Sub { get; init; }

    public bool Verify([NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrWhiteSpace(Sub))
        {
            error = "body.sub is missing";
            return false;
        }

        error = null;
        return true;
    }
}
