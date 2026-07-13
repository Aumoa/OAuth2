using System.Net;
using OAuth2.DataTransfer;

namespace OAuth2.Services;

internal sealed class HttpBackendClient(HttpClient http) : IBackendClient
{
    public async Task<bool> VerifyAccountIdAsync(string id, CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync(
            $"/api/v1/accounts?id={Uri.EscapeDataString(id)}",
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
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

        return PostAsync("/api/v1/accounts", form, acceptLanguage, cancellationToken);
    }

    public Task<BackendResponse> LoginAsync(
        LoginForm form,
        CancellationToken cancellationToken = default)
    {
        if (!form.Verify(out _))
        {
            throw new ArgumentException("Form verification failed.", nameof(form));
        }

        return PostAsync("/api/v1/accounts/login", form, null, cancellationToken);
    }

    public Task<BackendResponse> VerifyEmailAsync(
        EmailVerificationForm form,
        CancellationToken cancellationToken = default)
    {
        if (!form.Verify(out _))
        {
            throw new ArgumentException("Form verification failed.", nameof(form));
        }

        return PostAsync("/api/v1/accounts/email/verify", form, null, cancellationToken);
    }

    public Task<BackendResponse> ResendEmailVerificationAsync(
        EmailVerificationResendForm form,
        string? acceptLanguage,
        CancellationToken cancellationToken = default)
    {
        if (!form.Verify(out _))
        {
            throw new ArgumentException("Form verification failed.", nameof(form));
        }

        return PostAsync(
            "/api/v1/accounts/email/resend",
            form,
            acceptLanguage,
            cancellationToken);
    }

    public Task<BackendResponse> VerifyChallengeAsync(
        string code,
        string? state,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        code = $"code={Uri.EscapeDataString(code)}";
        state = state == null ? "" : $"&state={Uri.EscapeDataString(state)}";
        return GetAsync($"/api/v1/challenges?{code}{state}", null, cancellationToken);
    }

    private async Task<BackendResponse> GetAsync(
        string requestUri,
        string? acceptLanguage,
        CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(requestUri, cancellationToken);
        var content = response.Content.Headers.ContentLength == 0
            ? null
            : await response.Content.ReadAsStringAsync(cancellationToken);

        return new BackendResponse(
            response.StatusCode,
            content,
            response.Content.Headers.ContentType?.ToString());
    }

    private async Task<BackendResponse> PostAsync<T>(
        string requestUri,
        T body,
        string? acceptLanguage,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(body)
        };

        if (!string.IsNullOrWhiteSpace(acceptLanguage))
        {
            request.Headers.TryAddWithoutValidation("Accept-Language", acceptLanguage);
        }

        using var response = await http.SendAsync(request, cancellationToken);
        var content = response.Content.Headers.ContentLength == 0
            ? null
            : await response.Content.ReadAsStringAsync(cancellationToken);

        return new BackendResponse(
            response.StatusCode,
            content,
            response.Content.Headers.ContentType?.ToString());
    }
}
