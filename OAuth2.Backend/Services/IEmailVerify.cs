using System.Net.Mail;

namespace OAuth2.Services;

public interface IEmailVerify
{
    Task SendAsync(string sub, string verifyCode, MailAddress sendTo, CancellationToken cancellationToken = default);
}
