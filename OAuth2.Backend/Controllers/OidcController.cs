using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuth2.Data;
using OAuth2.DataTransfer;
using OAuth2.OpenId;
using OAuth2.Options;
using OAuth2.Repositories;
using OAuth2.Services;

namespace OAuth2.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/v1/oidc")]
public sealed class OidcController(
    OidcAuthorizationRequestValidator authorizationValidator,
    IAuthorizationCodes authorizationCodes,
    IApplications applications,
    IApplicationSecrets applicationSecrets,
    IRefreshTokens refreshTokens,
    IAccounts accounts,
    IAccountClaims accountClaims,
    IOrganizationMembers organizationMembers,
    OidcTokenIssuer tokenIssuer,
    OidcSigningKey signingKey,
    IOptions<OAuthOptions> oauthOptions) : ControllerBase
{
    [HttpPost("authorization-requests/validation")]
    public async Task<IActionResult> ValidateAuthorizationRequestAsync(
        [FromBody] OidcAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await authorizationValidator.ValidateAsync(request, cancellationToken));
    }

    [HttpPost("token")]
    public async Task<IActionResult> ExchangeAuthorizationCodeAsync(
        [FromBody] OidcTokenExchange exchange,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(exchange.Code)
            || string.IsNullOrWhiteSpace(exchange.ClientId)
            || string.IsNullOrWhiteSpace(exchange.RedirectUri)
            || string.IsNullOrWhiteSpace(exchange.CodeVerifier))
        {
            return InvalidGrant();
        }

        var application = await AuthenticateApplicationAsync(
            exchange.ClientId,
            exchange.ClientSecret,
            cancellationToken);
        if (application is null)
        {
            return InvalidClient();
        }

        var authorizationCode = await authorizationCodes.PopAsync(
            exchange.Code,
            cancellationToken);
        if (!authorizationCode.HasValue)
        {
            return InvalidGrant();
        }

        var code = authorizationCode.Value;
        if (string.Equals(code.ClientId, oauthOptions.Value.ClientId, StringComparison.Ordinal)
            || !string.Equals(exchange.ClientId, code.ClientId, StringComparison.Ordinal)
            || !string.Equals(exchange.RedirectUri, code.RedirectUri, StringComparison.Ordinal)
            || !Pkce.Validate(
                exchange.CodeVerifier,
                code.CodeChallenge,
                code.CodeChallengeMethod))
        {
            return InvalidGrant();
        }

        if (!OidcAuthorizationPolicy.TryValidateRegisteredApplication(
                new OidcAuthorizationRequest
                {
                    ClientId = code.ClientId,
                    RedirectUri = code.RedirectUri,
                    ResponseType = "code",
                    Scope = code.Scope,
                    State = "token-exchange",
                    Prompt = OidcScopePolicy.Split(code.Scope)
                        .Contains(OidcScopePolicy.OfflineAccessScope, StringComparer.Ordinal)
                        ? OidcAuthorizationPolicy.ConsentPrompt
                        : null,
                    CodeChallenge = code.CodeChallenge,
                    CodeChallengeMethod = code.CodeChallengeMethod
                },
                application,
                out var normalizedScope,
                out _,
                out _)
            || !string.Equals(normalizedScope, code.Scope, StringComparison.Ordinal))
        {
            return InvalidGrant();
        }

        var account = await accounts.GetAccountAsync(code.AccountId, cancellationToken);
        if (account is null
            || !string.IsNullOrEmpty(account.VerifyCode)
            || string.IsNullOrWhiteSpace(account.Id)
            || string.IsNullOrWhiteSpace(account.Sub)
            || string.IsNullOrWhiteSpace(account.Email)
            || string.IsNullOrWhiteSpace(account.Name))
        {
            return InvalidGrant();
        }

        var claims = await accountClaims.GetClaimsAsync(code.AccountId, cancellationToken);
        var organizationClaims = await organizationMembers.GetClaimsAsync(
            code.AccountId,
            cancellationToken);
        var refreshToken = OidcScopePolicy.Split(code.Scope)
            .Contains(OidcScopePolicy.OfflineAccessScope, StringComparer.Ordinal)
            ? await refreshTokens.CreateAsync(
                code.AccountId,
                code.ClientId,
                code.Scope,
                code.AuthTime,
                cancellationToken)
            : null;
        return Ok(tokenIssuer.Issue(
            account,
            claims,
            organizationClaims,
            code.ClientId,
            code.Scope,
            code.AuthTime,
            code.Nonce,
            refreshToken));
    }

    [HttpPost("token/refresh")]
    public async Task<IActionResult> ExchangeRefreshTokenAsync(
        [FromBody] OidcRefreshTokenExchange exchange,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(exchange.RefreshToken)
            || exchange.RefreshToken.Length > IRefreshTokens.MaxTokenLength
            || string.IsNullOrWhiteSpace(exchange.ClientId))
        {
            return InvalidGrant();
        }
        if (exchange.Scope?.Length > IRefreshTokens.MaxScopeLength)
        {
            return BadRequest(new { error = "invalid_scope" });
        }

        var application = await AuthenticateApplicationAsync(
            exchange.ClientId,
            exchange.ClientSecret,
            cancellationToken);
        if (application is null)
        {
            return InvalidClient();
        }

        var rotation = await refreshTokens.RotateAsync(
            exchange.ClientId,
            exchange.RefreshToken,
            exchange.Scope,
            cancellationToken);
        if (rotation.Status == RefreshTokenRotationStatus.InvalidScope)
        {
            return BadRequest(new { error = "invalid_scope" });
        }

        if (rotation.Status != RefreshTokenRotationStatus.Succeeded
            || string.IsNullOrWhiteSpace(rotation.Token)
            || string.IsNullOrWhiteSpace(rotation.AccountId)
            || string.IsNullOrWhiteSpace(rotation.ClientId)
            || string.IsNullOrWhiteSpace(rotation.Scope)
            || string.IsNullOrWhiteSpace(rotation.GrantedScope))
        {
            return InvalidGrant();
        }

        if (OidcScopePolicy.Split(rotation.GrantedScope)
            .Except(application.AllowedScopes, StringComparer.Ordinal)
            .Any())
        {
            await refreshTokens.RevokeAsync(
                exchange.ClientId,
                rotation.Token,
                cancellationToken);
            return InvalidGrant();
        }

        var account = await accounts.GetAccountAsync(rotation.AccountId, cancellationToken);
        if (!IsEligibleAccount(account))
        {
            await refreshTokens.RevokeAsync(
                exchange.ClientId,
                rotation.Token,
                cancellationToken);
            return InvalidGrant();
        }

        var claims = await accountClaims.GetClaimsAsync(
            rotation.AccountId,
            cancellationToken);
        var organizationClaims = await organizationMembers.GetClaimsAsync(
            rotation.AccountId,
            cancellationToken);
        return Ok(tokenIssuer.Issue(
            account!,
            claims,
            organizationClaims,
            rotation.ClientId,
            rotation.Scope,
            rotation.AuthTime,
            null,
            rotation.Token));
    }

    [HttpPost("token/revocation")]
    public async Task<IActionResult> RevokeTokenAsync(
        [FromBody] OidcTokenRevocation revocation,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(revocation.Token)
            || string.IsNullOrWhiteSpace(revocation.ClientId))
        {
            return BadRequest(new { error = "invalid_request" });
        }

        if (await AuthenticateApplicationAsync(
                revocation.ClientId,
                revocation.ClientSecret,
                cancellationToken) is null)
        {
            return InvalidClient();
        }

        await refreshTokens.RevokeAsync(
            revocation.ClientId,
            revocation.Token,
            cancellationToken);
        return Ok();
    }

    [HttpPost("userinfo")]
    public IActionResult GetUserInfo([FromBody] OidcAccessTokenRequest request)
    {
        return tokenIssuer.TryGetUserInfo(request.Token, out var userInfo)
            ? Ok(userInfo)
            : Unauthorized(new { error = "invalid_token" });
    }

    [HttpGet("jwks")]
    public IActionResult GetJsonWebKeys()
    {
        return Ok(signingKey.GetJsonWebKeySet());
    }

    private BadRequestObjectResult InvalidGrant()
    {
        return BadRequest(new { error = "invalid_grant" });
    }

    private UnauthorizedObjectResult InvalidClient()
    {
        return Unauthorized(new { error = "invalid_client" });
    }

    private async Task<OAuthApplicationConfiguration?> AuthenticateApplicationAsync(
        string clientId,
        string? clientSecret,
        CancellationToken cancellationToken)
    {
        var application = await applications.GetApplicationAsync(clientId, cancellationToken);
        if (application is null)
        {
            return null;
        }

        if (application.Application.RequiresSecret)
        {
            return !string.IsNullOrWhiteSpace(clientSecret)
                && clientSecret.Length <= 256
                && await applicationSecrets.VerifyAsync(
                    clientId,
                    clientSecret,
                    cancellationToken)
                ? application
                : null;
        }

        return clientSecret is null ? application : null;
    }

    private static bool IsEligibleAccount(Account? account) =>
        account is not null
        && string.IsNullOrEmpty(account.VerifyCode)
        && !string.IsNullOrWhiteSpace(account.Id)
        && !string.IsNullOrWhiteSpace(account.Sub)
        && !string.IsNullOrWhiteSpace(account.Email)
        && !string.IsNullOrWhiteSpace(account.Name);
}
