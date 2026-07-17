using Microsoft.AspNetCore.Mvc;
using OAuth2.Data;
using OAuth2.DataTransfer;
using OAuth2.Repositories;

namespace OAuth2.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/v1/applications/{clientId}/secrets")]
public sealed class ApplicationSecretsController(
    IApplications applications,
    IApplicationSecrets applicationSecrets) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromRoute] string clientId,
        [FromQuery] string ownerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(ownerId))
        {
            return BadRequest();
        }

        var application = await applications.GetOwnedApplicationAsync(
            clientId,
            ownerId,
            cancellationToken);
        if (application is null)
        {
            return NotFound();
        }

        if (application.Application.ApplicationType != OAuthApplicationTypes.Web)
        {
            return Conflict("Client secrets are available only to web applications.");
        }

        var existingSecrets = await applicationSecrets.GetOwnedAsync(
            clientId,
            ownerId,
            cancellationToken);
        if (existingSecrets.Count >= IApplicationSecrets.MaxActiveSecrets)
        {
            return Conflict("The active client secret limit has been reached.");
        }

        var generatedSecret = await applicationSecrets.AddAsync(
            clientId,
            ownerId,
            cancellationToken);
        if (generatedSecret is null)
        {
            return Conflict();
        }

        return Created(
            $"/api/v1/applications/{Uri.EscapeDataString(clientId)}/secrets/{generatedSecret.ApplicationSecret.Id}",
            ToCreated(generatedSecret));
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string clientId,
        [FromQuery] string ownerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(ownerId))
        {
            return BadRequest();
        }

        var application = await applications.GetOwnedApplicationAsync(
            clientId,
            ownerId,
            cancellationToken);
        if (application is null)
        {
            return NotFound();
        }

        var secrets = await applicationSecrets.GetOwnedAsync(
            clientId,
            ownerId,
            cancellationToken);
        return Ok(secrets.Select(ToSummary).ToArray());
    }

    [HttpDelete("{secretId:long}")]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] string clientId,
        [FromRoute] long secretId,
        [FromQuery] string ownerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(ownerId)
            || secretId <= 0)
        {
            return BadRequest();
        }

        return await applicationSecrets.DeleteAsync(
            clientId,
            ownerId,
            secretId,
            cancellationToken)
            ? NoContent()
            : NotFound();
    }

    private static ApplicationSecretSummary ToSummary(OAuthApplicationSecret secret) => new()
    {
        Id = secret.Id,
        Prefix = secret.Prefix,
        CreatedAt = secret.CreatedAt
    };

    private static CreatedApplicationSecret ToCreated(GeneratedOAuthApplicationSecret secret) => new()
    {
        Id = secret.ApplicationSecret.Id,
        Prefix = secret.ApplicationSecret.Prefix,
        CreatedAt = secret.ApplicationSecret.CreatedAt,
        Secret = secret.Value
    };
}
