namespace OAuth2.DataTransfer;

public sealed record OrganizationMemberPage
{
    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;

    public required IReadOnlyList<OrganizationMemberSummary> Items { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public long TotalCount { get; init; }
}
