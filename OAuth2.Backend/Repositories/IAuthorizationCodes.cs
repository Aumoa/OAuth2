namespace OAuth2.Repositories;

public interface IAuthorizationCodes
{
    ValueTask<string> PushAsync(
        AuthorizationCodeBody body,
        CancellationToken cancellationToken = default);

    ValueTask<AuthorizationCodeBody?> PopAsync(
        string code,
        CancellationToken cancellationToken = default);
}
