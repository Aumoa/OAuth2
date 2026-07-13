using System.Net.Mail;
using System.Text.Encodings.Web;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using OAuth2.Localizational;
using OAuth2.Options;

namespace OAuth2.Services;

internal sealed class SESEmailVerify(
    IAmazonSimpleEmailService client,
    IOptions<SESOptions> options,
    IStringLocalizer<Strings> localizer) : IEmailVerify
{
    private const string Charset = "UTF-8";

    public async Task SendAsync(
        string sub,
        string verifyCode,
        MailAddress sendTo,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sub);
        ArgumentException.ThrowIfNullOrWhiteSpace(verifyCode);
        ArgumentNullException.ThrowIfNull(sendTo);

        var settings = options.Value;
        var verificationUrl = QueryHelpers.AddQueryString(
            settings.VerificationUrl,
            new Dictionary<string, string?>
            {
                ["sub"] = sub,
                ["code"] = verifyCode
            });

        var subject = Localize("SEND_EMAILVERIFY_SUBJECT", settings.SenderName);
        var heading = Localize("SEND_EMAILVERIFY_BODY_HTML_HEAD");
        var message = Localize("SEND_EMAILVERIFY_BODY_HTML_MESSAGE");
        var hint = Localize(
            "SEND_EMAILVERIFY_BODY_HTML_HINT_MESSAGE",
            settings.VerificationCodeValidMinutes);
        var buttonText = Localize("SEND_EMAILVERIFY_BODY_HTML_BUTTON");
        var copyInstruction = Localize("SEND_EMAILVERIFY_BODY_HTML_COPY_INSTRUCTION");
        var ignoreMessage = Localize("SEND_EMAILVERIFY_BODY_HTML_IGNORE_MESSAGE");

        var request = new SendEmailRequest
        {
            Source = CreateSource(settings),
            Destination = new Destination
            {
                ToAddresses = [sendTo.Address]
            },
            Message = new Message
            {
                Subject = CreateContent(subject),
                Body = new Body
                {
                    Html = CreateContent(BuildHtml(
                        heading,
                        message,
                        hint,
                        buttonText,
                        copyInstruction,
                        ignoreMessage,
                        verificationUrl)),
                    Text = CreateContent(BuildText(
                        heading,
                        message,
                        hint,
                        buttonText,
                        ignoreMessage,
                        verificationUrl))
                }
            }
        };

        await client.SendEmailAsync(request, cancellationToken);
    }

    private string Localize(string name, params object[] arguments)
    {
        var localized = arguments.Length == 0
            ? localizer[name]
            : localizer[name, arguments];
        if (localized.ResourceNotFound)
        {
            throw new InvalidOperationException(
                $"Email localization resource '{name}' was not found for culture " +
                $"'{System.Globalization.CultureInfo.CurrentUICulture.Name}'.");
        }

        return localized.Value;
    }

    private static string CreateSource(SESOptions settings)
    {
        return string.IsNullOrWhiteSpace(settings.SenderName)
            ? settings.SenderAddress
            : new MailAddress(settings.SenderAddress, settings.SenderName).ToString();
    }

    private static Content CreateContent(string data) => new()
    {
        Charset = Charset,
        Data = data
    };

    private static string BuildText(
        string heading,
        string message,
        string hint,
        string buttonText,
        string ignoreMessage,
        string verificationUrl) =>
        $"{heading}\n\n{message}\n{hint}\n\n{buttonText}: {verificationUrl}\n\n{ignoreMessage}";

    private static string BuildHtml(
        string heading,
        string message,
        string hint,
        string buttonText,
        string copyInstruction,
        string ignoreMessage,
        string verificationUrl)
    {
        var encoder = HtmlEncoder.Default;
        heading = encoder.Encode(heading);
        message = encoder.Encode(message);
        hint = encoder.Encode(hint);
        buttonText = encoder.Encode(buttonText);
        copyInstruction = encoder.Encode(copyInstruction);
        ignoreMessage = encoder.Encode(ignoreMessage);
        verificationUrl = encoder.Encode(verificationUrl);

        return $$"""
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background-color:#f6f8fb;font-family:Arial,'Apple SD Gothic Neo',sans-serif;">
                <tr>
                    <td align="center" style="padding:40px 16px;">
                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="max-width:560px;background-color:#ffffff;border:1px solid #d8dee7;border-radius:12px;">
                            <tr>
                                <td style="padding:40px 32px;">
                                    <h1 style="margin:0 0 20px;color:#1f2937;font-size:24px;line-height:1.4;">{{heading}}</h1>
                                    <p style="margin:0 0 16px;color:#64748b;font-size:16px;line-height:1.7;">{{message}}</p>
                                    <p style="margin:0 0 28px;color:#64748b;font-size:14px;line-height:1.7;">{{hint}}</p>
                                    <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin:0 auto 28px;">
                                        <tr>
                                            <td align="center" bgcolor="#0f766e" style="border-radius:8px;">
                                                <a href="{{verificationUrl}}" style="display:inline-block;padding:14px 28px;color:#ffffff;font-size:16px;font-weight:bold;text-decoration:none;">{{buttonText}}</a>
                                            </td>
                                        </tr>
                                    </table>
                                    <p style="margin:0 0 8px;color:#64748b;font-size:13px;line-height:1.6;">{{copyInstruction}}</p>
                                    <p style="margin:0;padding:12px;overflow-wrap:anywhere;background-color:#eef2f7;border-radius:6px;font-size:12px;line-height:1.6;">
                                        <a href="{{verificationUrl}}" style="color:#0f766e;">{{verificationUrl}}</a>
                                    </p>
                                    <p style="margin:28px 0 0;color:#64748b;font-size:12px;line-height:1.6;">{{ignoreMessage}}</p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
            """;
    }
}
