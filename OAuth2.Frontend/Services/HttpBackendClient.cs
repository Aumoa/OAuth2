using OAuth2.DataTransfer;

namespace OAuth2.Services;

internal sealed class HttpBackendClient(HttpClient http) : IBackendClient
{
    public async Task<bool> VerifyAccountIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await http.GetAsync($"/api/v1/accounts?id={Uri.EscapeDataString(id)}", cancellationToken);
        return result.IsSuccessStatusCode;
    }

    public async Task<bool> RegisterAccountAsync(RegisterForm form, CancellationToken cancellationToken = default)
    {
        if (!form.Verify(out _))
        {
            throw new ArgumentException("Form verification failed.");
        }

        var result = await http.PostAsync($"/api/v1/accounts", JsonContent.Create(form), cancellationToken);
        return result.IsSuccessStatusCode;
    }
}
