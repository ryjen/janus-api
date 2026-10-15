#nullable enable

using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;

namespace Janus;

// Provider-agnostic login result; never expose AWS SDK response objects across
// the HTTP/application seam. A missing challenge name represents token delivery.
public sealed record LoginSessionResult(string? IdToken, string? ChallengeName, string? Session);

public interface IAuthenticationGateway
{
    Task<LoginSessionResult> AuthenticateAsync(
        string email, string password, CancellationToken cancellationToken = default);

    Task<string?> RespondToNewPasswordAsync(
        string email, string password, string session, CancellationToken cancellationToken = default);
}

// Request construction for the existing Cognito ADMIN_USER_PASSWORD_AUTH flow.
// Successful authentication and legacy challenge behavior remain unchanged.
public sealed class CognitoAuthenticationGateway : IAuthenticationGateway
{
    private const string UsernameParameter = "USERNAME";
    private const string PasswordParameter = "PASSWORD";
    private const string NewPasswordParameter = "NEW_PASSWORD";

    private readonly IAmazonCognitoIdentityProvider _client;
    private readonly string _poolId;
    private readonly string _clientId;

    public CognitoAuthenticationGateway(
        IAmazonCognitoIdentityProvider client, string poolId, string clientId)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _poolId = !string.IsNullOrWhiteSpace(poolId)
            ? poolId : throw new ArgumentException("User pool configuration required", nameof(poolId));
        _clientId = !string.IsNullOrWhiteSpace(clientId)
            ? clientId : throw new ArgumentException("Client configuration required", nameof(clientId));
    }

    public async Task<LoginSessionResult> AuthenticateAsync(
        string email, string password, CancellationToken cancellationToken = default)
    {
        var result = await _client.AdminInitiateAuthAsync(new AdminInitiateAuthRequest
        {
            UserPoolId = _poolId,
            ClientId = _clientId,
            AuthFlow = AuthFlowType.ADMIN_USER_PASSWORD_AUTH,
            AuthParameters = new Dictionary<string, string>
            {
                [UsernameParameter] = email,
                [PasswordParameter] = password
            }
        }, cancellationToken);

        return new LoginSessionResult(
            result.AuthenticationResult?.IdToken,
            result.ChallengeName?.ToString(),
            result.Session);
    }

    public async Task<string?> RespondToNewPasswordAsync(
        string email, string password, string session, CancellationToken cancellationToken = default)
    {
        var result = await _client.AdminRespondToAuthChallengeAsync(
            new AdminRespondToAuthChallengeRequest
            {
                ChallengeName = ChallengeNameType.NEW_PASSWORD_REQUIRED,
                ClientId = _clientId,
                UserPoolId = _poolId,
                ChallengeResponses = new Dictionary<string, string>
                {
                    [UsernameParameter] = email,
                    [NewPasswordParameter] = password
                },
                Session = session
            }, cancellationToken);
        return result.AuthenticationResult?.IdToken;
    }
}
