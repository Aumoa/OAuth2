using System.Diagnostics.CodeAnalysis;
using OAuth2.Data;

namespace OAuth2.DataTransfer;

public sealed record UpdateAccountProfileForm
{
    public const int FullNameMaxLength = 128;

    public const int NicknameMaxLength = 128;

    public const int ClaimValueMaxLength = 2048;

    public required string FullName { get; init; }

    public string? Nickname { get; init; }

    public required IReadOnlyList<AccountProfileClaim> Claims { get; init; }

    public bool Verify([NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrWhiteSpace(FullName))
        {
            error = "body.fullName is missing";
            return false;
        }

        if (FullName.Trim().Length > FullNameMaxLength)
        {
            error = $"body.fullName exceeds {FullNameMaxLength} characters";
            return false;
        }

        if (Nickname?.Trim().Length > NicknameMaxLength)
        {
            error = $"body.nickname exceeds {NicknameMaxLength} characters";
            return false;
        }

        if (Claims is null)
        {
            error = "body.claims is missing";
            return false;
        }

        if (Claims.Count > AccountProfileClaimTypes.Additional.Count)
        {
            error = $"body.claims exceeds {AccountProfileClaimTypes.Additional.Count} entries";
            return false;
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var claim in Claims)
        {
            if (claim is null || !AccountProfileClaimTypes.IsAdditional(claim.Name))
            {
                error = "body.claims contains an unsupported claim type";
                return false;
            }

            if (!names.Add(claim.Name))
            {
                error = "body.claims contains a duplicate claim type";
                return false;
            }

            if (string.IsNullOrWhiteSpace(claim.Value))
            {
                error = "body.claims contains an empty value";
                return false;
            }

            if (claim.Value.Trim().Length > ClaimValueMaxLength)
            {
                error = $"body.claims contains a value exceeding {ClaimValueMaxLength} characters";
                return false;
            }
        }

        error = null;
        return true;
    }
}
