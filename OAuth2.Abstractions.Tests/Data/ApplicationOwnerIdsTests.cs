using OAuth2.Data;

namespace OAuth2.Abstractions.Tests.Data;

public sealed class ApplicationOwnerIdsTests
{
    [Fact]
    public void CreateForOrganization_IsStableAndNamespaced()
    {
        var first = ApplicationOwnerIds.CreateForOrganization("example");
        var second = ApplicationOwnerIds.CreateForOrganization("example");

        Assert.Equal(first, second);
        Assert.Equal(
            "organization:50d858e0985ecc7f60418aaf0cc5ab587f42c2570a884095a9e8ccacd0f6545c",
            first);
    }

    [Fact]
    public void CreateForOrganization_SeparatesOrganizationIds()
    {
        Assert.NotEqual(
            ApplicationOwnerIds.CreateForOrganization("example-a"),
            ApplicationOwnerIds.CreateForOrganization("example-b"));
    }

    [Theory]
    [InlineData("organization:abc")]
    [InlineData("Organization:abc")]
    public void IsReservedAccountId_ProtectsOrganizationNamespace(string accountId)
    {
        Assert.True(ApplicationOwnerIds.IsReservedAccountId(accountId));
        Assert.False(ApplicationOwnerIds.IsReservedAccountId("ordinary-account"));
    }
}
