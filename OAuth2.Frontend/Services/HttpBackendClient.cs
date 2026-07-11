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

    public async Task<HttpStatusCode> RegisterAccountAsync(
        RegisterForm form,
        string? acceptLanguage,
        CancellationToken cancellationToken = default)
    {
        if (!form.Verify(out _))
        {
            throw new ArgumentException("Form verification failed.", nameof(form));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/accounts")
        {
            Content = JsonContent.Create(form)
        };

        if (!string.IsNullOrWhiteSpace(acceptLanguage))
        {
            request.Headers.TryAddWithoutValidation("Accept-Language", acceptLanguage);
        }

        using var response = await http.SendAsync(request, cancellationToken);
        return response.StatusCode;
    }
}
