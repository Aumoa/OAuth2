namespace OAuth2.Data;

public sealed record OrganizationMembership
{
    public required OAuthOrganization Organization { get; init; }

    public required string Role { get; init; }
}
