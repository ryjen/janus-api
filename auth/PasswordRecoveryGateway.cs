#nullable enable

using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;

namespace Janus;

// The application requests Cognito password recovery but never issues tokens
// or treats a caller-provided recovery code as proof of identity itself.
public interface IPasswordRecoveryGateway
{
    Task StartAsync(string email, CancellationToken cancellationToken = default);
    Task ConfirmAsync(string email, string code, string newPassword, CancellationToken cancellationToken = default);
}

// AWS SDK adapter; accepts opaque password values unchanged.
public sealed class CognitoPasswordRecoveryGateway : IPasswordRecoveryGateway
{
    private readonly IAmazonCognitoIdentityProvider _client;
    private readonly string _clientId;

    public CognitoPasswordRecoveryGateway(IAmazonCognitoIdentityProvider client, string clientId)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _clientId = !string.IsNullOrWhiteSpace(clientId)
            ? clientId : throw new ArgumentException("Client configuration required", nameof(clientId));
    }

    public async Task StartAsync(string email, CancellationToken cancellationToken = default)
    {
        await _client.ForgotPasswordAsync(new ForgotPasswordRequest
        {
            ClientId = _clientId,
            Username = email
        }, cancellationToken);
    }

    public async Task ConfirmAsync(
        string email, string code, string newPassword, CancellationToken cancellationToken = default)
    {
        await _client.ConfirmForgotPasswordAsync(new ConfirmForgotPasswordRequest
        {
            ClientId = _clientId,
            Username = email,
            ConfirmationCode = code,
            Password = newPassword
        }, cancellationToken);
    }
}
