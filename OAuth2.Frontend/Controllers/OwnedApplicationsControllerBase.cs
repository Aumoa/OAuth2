using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuth2.Services;
using BffSessionOptions = OAuth2.Options.SessionOptions;

namespace OAuth2.Controllers;

public abstract class OwnedApplicationsControllerBase(
    ISessionsRepository sessions,
    IBackendClient backend,
    IOptions<BffSessionOptions> sessionOptions) : BackendProxyControllerBase
{
    protected IBackendClient Backend { get; } = backend;

    protected async Task<OwnerResolution> ResolveOwnerAsync(
        string? organizationId,
        CancellationToken cancellationToken)
    {
        var accountId = await GetCurrentAccountIdAsync(
            sessions,
            sessionOptions.Value.CookieName,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return new(OwnerResolutionStatus.Unauthorized, null);
        }

        if (organizationId is null)
        {
            return new(OwnerResolutionStatus.Success, accountId);
        }

        if (string.IsNullOrWhiteSpace(organizationId))
        {
            return new(OwnerResolutionStatus.Forbidden, null);
        }

        var organization = await Backend.GetOrganizationAsync(
            accountId,
            organizationId,
            cancellationToken);
        if (organization.StatusCode == HttpStatusCode.NotFound)
        {
            return new(OwnerResolutionStatus.Forbidden, null);
        }

        if (organization.StatusCode is < HttpStatusCode.OK or >= HttpStatusCode.MultipleChoices)
        {
            return new(OwnerResolutionStatus.BackendFailure, null, organization);
        }

        return new(
            OwnerResolutionStatus.Success,
            CreateOrganizationOwnerId(organizationId));
    }

    protected IActionResult? OwnerResolutionError(OwnerResolution owner) => owner.Status switch
    {
        OwnerResolutionStatus.Success => null,
        OwnerResolutionStatus.Unauthorized => Unauthorized(),
        OwnerResolutionStatus.Forbidden => Forbid(),
        OwnerResolutionStatus.BackendFailure => FromBackend(owner.BackendResponse!),
        _ => throw new ArgumentOutOfRangeException(nameof(owner))
    };

    private static string CreateOrganizationOwnerId(string organizationId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(organizationId));
        return $"organization:{Convert.ToHexStringLower(hash)}";
    }

    protected enum OwnerResolutionStatus
    {
        Success,
        Unauthorized,
        Forbidden,
        BackendFailure
    }

    protected readonly record struct OwnerResolution(
        OwnerResolutionStatus Status,
        string? OwnerId,
        BackendResponse? BackendResponse = null);
}
