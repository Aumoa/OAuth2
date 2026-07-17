using OAuth2.Data;
using OAuth2.OpenId;

namespace OAuth2.Abstractions.Tests.OpenId;

public sealed class OidcRedirectUriPolicyTests
{
    [Theory]
    [InlineData(OAuthApplicationTypes.Web, "https://web.example/callback")]
    [InlineData(OAuthApplicationTypes.Web, "http://localhost:5173/callback")]
    [InlineData(OAuthApplicationTypes.Android, "https://app.example/android/callback")]
    [InlineData(OAuthApplicationTypes.Android, "com.example.android:/oauth/callback")]
    [InlineData(OAuthApplicationTypes.Ios, "https://app.example/ios/callback")]
    [InlineData(OAuthApplicationTypes.Ios, "com.example.ios:/oauth/callback")]
    [InlineData(OAuthApplicationTypes.MacOs, "https://app.example/macos/callback")]
    [InlineData(OAuthApplicationTypes.MacOs, "com.example.macos:/oauth/callback")]
    [InlineData(OAuthApplicationTypes.MacOs, "http://127.0.0.1/oauth/callback")]
    [InlineData(OAuthApplicationTypes.MacOs, "http://[::1]/oauth/callback")]
    public void IsValidRegistration_AcceptsRedirectForApplicationType(
        string applicationType,
        string redirectUri)
    {
        Assert.True(OidcRedirectUriPolicy.IsValidRegistration(
            redirectUri,
            applicationType));
    }

    [Theory]
    [InlineData(OAuthApplicationTypes.Web, "com.example.web:/oauth/callback")]
    [InlineData(OAuthApplicationTypes.Android, "http://127.0.0.1:42000/oauth/callback")]
    [InlineData(OAuthApplicationTypes.Ios, "http://127.0.0.1:42000/oauth/callback")]
    [InlineData(OAuthApplicationTypes.MacOs, "http://localhost/oauth/callback")]
    [InlineData(OAuthApplicationTypes.MacOs, "http://127.0.0.1:42000/oauth/callback")]
    [InlineData(OAuthApplicationTypes.Android, "myapp:/oauth/callback")]
    public void IsValidRegistration_RejectsRedirectForApplicationType(
        string applicationType,
        string redirectUri)
    {
        Assert.False(OidcRedirectUriPolicy.IsValidRegistration(
            redirectUri,
            applicationType));
    }

    [Theory]
    [InlineData("http://127.0.0.1:49152/oauth/callback")]
    [InlineData("http://127.0.0.1:62001/oauth/callback")]
    [InlineData("http://[::1]:53000/oauth/callback")]
    public void Matches_AcceptsDynamicMacOsLoopbackPort(string requestedRedirectUri)
    {
        var registeredRedirectUri = requestedRedirectUri.Contains("[::1]", StringComparison.Ordinal)
            ? "http://[::1]/oauth/callback"
            : "http://127.0.0.1/oauth/callback";

        Assert.True(OidcRedirectUriPolicy.Matches(
            requestedRedirectUri,
            registeredRedirectUri,
            OAuthApplicationTypes.MacOs));
    }

    [Theory]
    [InlineData("http://127.0.0.2:49152/oauth/callback")]
    [InlineData("http://127.0.0.1:49152/another/callback")]
    [InlineData("http://127.0.0.1:49152/oauth/callback?unexpected=1")]
    public void Matches_RejectsMacOsLoopbackChangesOtherThanPort(
        string requestedRedirectUri)
    {
        Assert.False(OidcRedirectUriPolicy.Matches(
            requestedRedirectUri,
            "http://127.0.0.1/oauth/callback",
            OAuthApplicationTypes.MacOs));
    }
}
