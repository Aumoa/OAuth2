using System.Net;
using System.Text.Json;
using OAuth2.DataTransfer;

namespace OAuth2.Services;

internal sealed class HttpBackendClient(HttpClient http) : IBackendClient
{
    private static readonly JsonSerializerOptions s_JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<bool> AccountExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        using var request = new HttpRequestMessage(
            HttpMethod.Head,
            $"/api/v1/accounts/{Uri.EscapeDataString(id)}");
        using var response = await http.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    public Task<BackendResponse> CreateApplicationAsync(
        string ownerId,
        CreateApplicationForm form,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentNullException.ThrowIfNull(form);

        if (!form.Verify(out _))
        {
            throw new ArgumentException("Form verification failed.", nameof(form));
        }

        return SendAsync(
            HttpMethod.Post,
            $"/api/v1/applications?ownerId={Uri.EscapeDataString(ownerId)}",
            form,
            null,
            cancellationToken);
    }

    public Task<BackendResponse> CreateOrganizationAsync(
        string accountId,
        CreateOrganizationForm form,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentNullException.ThrowIfNull(form);

        if (!form.Verify(out _))
        {
            throw new ArgumentException("Form verification failed.", nameof(form));
        }

        return SendAsync(
            HttpMethod.Post,
            $"/api/v1/organizations?accountId={Uri.EscapeDataString(accountId)}",
            form,
            null,
            cancellationToken);
    }

    public async Task<BackendResponse> DeleteApplicationAsync(
        string ownerId,
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        using var response = await http.DeleteAsync(
            ApplicationUri(ownerId, id),
            cancellationToken);
        return await ToBackendResponseAsync(response, cancellationToken);
    }

    public async Task<BackendResponse> GetOwnedApplicationAsync(
        string ownerId,
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        using var response = await http.GetAsync(
            ApplicationUri(ownerId, id),
            cancellationToken);
        return await ToBackendResponseAsync(response, cancellationToken);
    }

    public async Task<BackendResponse> GetOwnedApplicationsAsync(
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        using var response = await http.GetAsync(
            $"/api/v1/applications?ownerId={Uri.EscapeDataString(ownerId)}",
            cancellationToken);
        return await ToBackendResponseAsync(response, cancellationToken);
    }

    public async Task<BackendResponse> GetOrganizationAsync(
        string accountId,
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        using var response = await http.GetAsync(
            $"/api/v1/organizations/{Uri.EscapeDataString(id)}?accountId={Uri.EscapeDataString(accountId)}",
            cancellationToken);
        return await ToBackendResponseAsync(response, cancellationToken);
    }

    public async Task<BackendResponse> GetOrganizationsAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        using var response = await http.GetAsync(
            $"/api/v1/organizations?accountId={Uri.EscapeDataString(accountId)}",
            cancellationToken);
        return await ToBackendResponseAsync(response, cancellationToken);
    }

    public Task<BackendResponse> UpdateApplicationAsync(
        string ownerId,
        string id,
        UpdateApplicationForm form,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(form);

        if (!form.Verify(out _))
        {
            throw new ArgumentException("Form verification failed.", nameof(form));
        }

        return SendAsync(
            HttpMethod.Put,
            ApplicationUri(ownerId, id),
            form,
            null,
            cancellationToken);
    }

    public Task<BackendResponse> RegisterAccountAsync(
        RegisterForm form,
        string? acceptLanguage,
        CancellationToken cancellationToken = default)
    {
        if (!form.Verify(out _))
        {
            throw new ArgumentException("Form verification failed.", nameof(form));
        }

        return SendAsync(
            HttpMethod.Post,
            "/api/v1/accounts",
            form,
            acceptLanguage,
            cancellationToken);
    }

    public Task<BackendResponse> CreateAuthorizationCodeAsync(
        LoginForm form,
        CancellationToken cancellationToken = default)
    {
        if (!form.Verify(out _))
        {
            throw new ArgumentException("Form verification failed.", nameof(form));
        }

        return SendAsync(
            HttpMethod.Post,
            "/api/v1/authorization-codes",
            form,
            null,
            cancellationToken);
    }

    public Task<BackendResponse> CreateAuthorizationCodeFromRememberedSessionAsync(
        RememberedLoginForm form,
        CancellationToken cancellationToken = default)
    {
        if (!form.Verify(out _))
        {
            throw new ArgumentException("Form verification failed.", nameof(form));
        }

        return SendAsync(
            HttpMethod.Post,
            "/api/v1/authorization-codes/remembered",
            form,
            null,
            cancellationToken);
    }

    public Task<BackendResponse> RevokeRememberedSessionAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        return SendAsync(
            HttpMethod.Delete,
            "/api/v1/remembered-sessions",
            new RememberedSessionRevocation { Token = token },
            null,
            cancellationToken);
    }

    public Task<BackendResponse> VerifyEmailAsync(
        EmailVerificationForm form,
        CancellationToken cancellationToken = default)
    {
        if (!form.Verify(out _))
        {
            throw new ArgumentException("Form verification failed.", nameof(form));
        }

        return SendAsync(
            HttpMethod.Put,
            "/api/v1/email-verifications",
            form,
            null,
            cancellationToken);
    }

    public Task<BackendResponse> CreateEmailVerificationDeliveryAsync(
        EmailVerificationResendForm form,
        string? acceptLanguage,
        CancellationToken cancellationToken = default)
    {
        if (!form.Verify(out _))
        {
            throw new ArgumentException("Form verification failed.", nameof(form));
        }

        return SendAsync(
            HttpMethod.Post,
            "/api/v1/email-verification-deliveries",
            form,
            acceptLanguage,
            cancellationToken);
    }

    public async Task<BackendResponse<GrantedUserInfo>> ExchangeAuthorizationCodeAsync(
        AuthorizationCodeExchange exchange,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exchange);

        using var response = await http.PostAsJsonAsync(
            "/api/v1/authorization-grants",
            exchange,
            cancellationToken);
        var content = response.Content.Headers.ContentLength == 0
            ? null
            : await response.Content.ReadAsStringAsync(cancellationToken);
        var value = response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(content)
            ? JsonSerializer.Deserialize<GrantedUserInfo>(content, s_JsonOptions)
            : null;

        return new BackendResponse<GrantedUserInfo>(
            response.StatusCode,
            value,
            content,
            response.Content.Headers.ContentType?.ToString());
    }

    private async Task<BackendResponse> SendAsync<T>(
        HttpMethod method,
        string requestUri,
        T body,
        string? acceptLanguage,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, requestUri)
        {
            Content = JsonContent.Create(body)
        };

        if (!string.IsNullOrWhiteSpace(acceptLanguage))
        {
            request.Headers.TryAddWithoutValidation("Accept-Language", acceptLanguage);
        }

        using var response = await http.SendAsync(request, cancellationToken);
        return await ToBackendResponseAsync(response, cancellationToken);
    }

    private static string ApplicationUri(string ownerId, string id) =>
        $"/api/v1/applications/{Uri.EscapeDataString(id)}?ownerId={Uri.EscapeDataString(ownerId)}";

    private static async Task<BackendResponse> ToBackendResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var content = response.Content.Headers.ContentLength == 0
            ? null
            : await response.Content.ReadAsStringAsync(cancellationToken);

        return new BackendResponse(
            response.StatusCode,
            content,
            response.Content.Headers.ContentType?.ToString());
    }
}
