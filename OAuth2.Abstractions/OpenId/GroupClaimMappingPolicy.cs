using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using OAuth2.Data;

namespace OAuth2.OpenId;

public static partial class GroupClaimMappingPolicy
{
    public const int MaxSelectors = 100;

    public const int MaxSelectorLength = 130;

    public const int MaxIdentifierLength = 64;

    public static GroupClaimMapping CreateDefault(string? ownerOrganizationId) => new()
    {
        Format = GroupClaimFormats.Dash,
        Selectors = string.IsNullOrWhiteSpace(ownerOrganizationId)
            ? []
            : [$"/{ownerOrganizationId}/*"]
    };

    public static bool TryNormalize(
        GroupClaimMapping? mapping,
        [NotNullWhen(true)] out GroupClaimMapping? normalizedMapping,
        [NotNullWhen(false)] out string? error)
    {
        normalizedMapping = null;
        if (mapping is null)
        {
            error = "group claim mapping is missing";
            return false;
        }

        var format = mapping.Format?.Trim();
        if (string.IsNullOrWhiteSpace(format) || !GroupClaimFormats.Supported.Contains(format))
        {
            error = "group claim mapping contains an unsupported format";
            return false;
        }

        if (mapping.Selectors is null)
        {
            error = "group claim mapping selectors are missing";
            return false;
        }

        if (mapping.Selectors.Count > MaxSelectors)
        {
            error = $"group claim mapping exceeds {MaxSelectors} selectors";
            return false;
        }

        var selectors = new List<string>(mapping.Selectors.Count);
        var uniqueSelectors = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in mapping.Selectors)
        {
            var selector = value?.Trim();
            if (string.IsNullOrWhiteSpace(selector)
                || selector.Length > MaxSelectorLength
                || !TryParseSelector(selector, out _))
            {
                error = "group claim mapping contains an invalid selector";
                return false;
            }

            if (!uniqueSelectors.Add(selector))
            {
                error = "group claim mapping contains a duplicate selector";
                return false;
            }

            selectors.Add(selector);
        }

        normalizedMapping = new GroupClaimMapping
        {
            Format = format,
            Selectors = selectors
        };
        error = null;
        return true;
    }

    public static IReadOnlyList<string> MapGroups(
        IReadOnlyList<OrganizationClaimValue> organizationClaims,
        GroupClaimMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(organizationClaims);
        var normalizedMapping = NormalizeOrThrow(mapping);
        var values = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var selector in normalizedMapping.Selectors.Select(ParseSelector))
        {
            var organization = organizationClaims.FirstOrDefault(claim =>
                string.Equals(claim.Id, selector.OrganizationId, StringComparison.Ordinal));
            if (organization is null)
            {
                continue;
            }

            if (selector.GroupId is null)
            {
                values.Add(FormatOrganization(normalizedMapping.Format, organization.Id));
                continue;
            }

            var groupIds = selector.GroupId == "*"
                ? organization.GroupIds
                : organization.GroupIds.Where(groupId =>
                    string.Equals(groupId, selector.GroupId, StringComparison.Ordinal));
            foreach (var groupId in groupIds)
            {
                values.Add(FormatGroup(normalizedMapping.Format, organization.Id, groupId));
            }
        }

        return values.ToArray();
    }

    public static IReadOnlyList<OrganizationClaimValue> FilterOrganizations(
        IReadOnlyList<OrganizationClaimValue> organizationClaims,
        GroupClaimMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(organizationClaims);
        var normalizedMapping = NormalizeOrThrow(mapping);
        var organizationIds = normalizedMapping.Selectors
            .Select(ParseSelector)
            .Select(static selector => selector.OrganizationId)
            .ToHashSet(StringComparer.Ordinal);
        return organizationClaims
            .Where(claim => organizationIds.Contains(claim.Id))
            .ToArray();
    }

    private static GroupClaimMapping NormalizeOrThrow(GroupClaimMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        if (!TryNormalize(mapping, out var normalizedMapping, out var error))
        {
            throw new ArgumentException(error, nameof(mapping));
        }

        return normalizedMapping;
    }

    private static string FormatOrganization(string format, string organizationId) => format switch
    {
        GroupClaimFormats.Dash or GroupClaimFormats.Colon => organizationId,
        GroupClaimFormats.Path => $"/{organizationId}",
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };

    private static string FormatGroup(string format, string organizationId, string groupId) =>
        format switch
        {
            GroupClaimFormats.Dash => $"{organizationId}-{groupId}",
            GroupClaimFormats.Path => $"/{organizationId}/{groupId}",
            GroupClaimFormats.Colon => $"{organizationId}:{groupId}",
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

    private static Selector ParseSelector(string value)
    {
        if (!TryParseSelector(value, out var selector))
        {
            throw new ArgumentException("The group claim selector is invalid.", nameof(value));
        }

        return selector;
    }

    private static bool TryParseSelector(
        string value,
        [NotNullWhen(true)] out Selector? selector)
    {
        selector = null;
        var match = SelectorPattern().Match(value);
        if (!match.Success)
        {
            return false;
        }

        var organizationId = match.Groups["organization"].Value;
        var groupId = match.Groups["group"].Success
            ? match.Groups["group"].Value
            : null;
        if (organizationId.Length > MaxIdentifierLength
            || groupId is { Length: > MaxIdentifierLength } and not "*")
        {
            return false;
        }

        selector = new Selector(organizationId, groupId);
        return true;
    }

    [GeneratedRegex(
        "^/(?<organization>[a-z0-9]+(?:-[a-z0-9]+)*)(?:/(?<group>\\*|[a-z0-9]+(?:-[a-z0-9]+)*))?$",
        RegexOptions.CultureInvariant)]
    private static partial Regex SelectorPattern();

    private sealed record Selector(string OrganizationId, string? GroupId);
}
