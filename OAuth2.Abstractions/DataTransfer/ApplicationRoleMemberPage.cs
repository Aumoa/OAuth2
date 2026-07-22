namespace OAuth2.DataTransfer;

public sealed record ApplicationRoleMemberPage
{
    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;

    public required IReadOnlyList<ApplicationRoleMemberSummary> Items { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public long TotalCount { get; init; }
}
