namespace OAuth2.Services;

public interface ISessionsRepository
{
    Task<bool> VerifySessionAsync();
}
