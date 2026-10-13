#nullable enable

using Amazon;
using Amazon.Runtime;
using Amazon.CognitoIdentityProvider;
using Amazon.Lambda.APIGatewayEvents;
using Janus;
using Xunit;

namespace Janus.Tests;

public sealed class AuthHttpParsingTests
{
    [Theory]
    [InlineData("/signup")]
    [InlineData("/confirm")]
    [InlineData("/authenticate")]
    [InlineData("/reset")]
    [InlineData("/password/forgot")]
    [InlineData("/password/confirm")]
    public async Task MalformedJsonIsClientError(string path)
    {
        using var cognito = new AmazonCognitoIdentityProviderClient(
            new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
        var auth = new Auth(cognito, "test-pool", "test-client");
        var result = await auth.Handler(new APIGatewayProxyRequest
        {
            Path = path,
            Body = "{not-json"
        }, null!);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task UnknownPathKeepsLegacyResponse()
    {
        using var cognito = new AmazonCognitoIdentityProviderClient(
            new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
        var auth = new Auth(cognito, "test-pool", "test-client");
        var result = await auth.Handler(new APIGatewayProxyRequest
        {
            Path = "/unsupported",
            Body = "{not-json"
        }, null!);
        Assert.Equal(401, result.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("\"text\"")]
    public async Task MissingOrNonObjectAuthBodyIs400(string? body)
    {
        using var cognito = new AmazonCognitoIdentityProviderClient(
            new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
        var auth = new Auth(cognito, "test-pool", "test-client");
        var result = await auth.Handler(new APIGatewayProxyRequest
        {
            Path = "/signup",
            Body = body
        }, null!);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task MalformedInputResponseDoesNotEchoSensitiveBody()
    {
        const string secret = "should-not-be-logged-or-echoed";
        using var cognito = new AmazonCognitoIdentityProviderClient(
            new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
        var auth = new Auth(cognito, "test-pool", "test-client");
        var result = await auth.Handler(new APIGatewayProxyRequest
        {
            Path = "/authenticate",
            Body = "{\"password\":\"" + secret
        }, null!);
        Assert.Equal(400, result.StatusCode);
        Assert.DoesNotContain(secret, result.Body);
    }
}
