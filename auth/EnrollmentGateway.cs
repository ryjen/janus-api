#nullable enable

using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;

namespace Janus;

// Application-facing capability: enrollment is requested, but an application
// account is provisioned only by the trusted PostConfirmation event path.
public interface IEnrollmentGateway
{
    Task RequestSignUpAsync(string email, string password, CancellationToken cancellationToken = default);
    Task ConfirmEmailAsync(string email, string confirmationCode, CancellationToken cancellationToken = default);
}

// AWS SDK adapter. Keep provider request construction outside the HTTP handler.
public sealed class CognitoEnrollmentGateway : IEnrollmentGateway
{
    private const string EmailAttributeName = "email";

    private readonly IAmazonCognitoIdentityProvider _client;
    private readonly string _clientId;

    public CognitoEnrollmentGateway(IAmazonCognitoIdentityProvider client, string clientId)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _clientId = !string.IsNullOrWhiteSpace(clientId)
            ? clientId : throw new ArgumentException("Client configuration required", nameof(clientId));
    }

    public async Task RequestSignUpAsync(
        string email, string password, CancellationToken cancellationToken = default)
    {
        await _client.SignUpAsync(new SignUpRequest
        {
            ClientId = _clientId,
            Username = email,
            Password = password,
            UserAttributes = new List<AttributeType>
            {
                new() { Name = EmailAttributeName, Value = email }
            }
        }, cancellationToken);
    }

    public async Task ConfirmEmailAsync(
        string email, string confirmationCode, CancellationToken cancellationToken = default)
    {
        await _client.ConfirmSignUpAsync(new ConfirmSignUpRequest
        {
            ClientId = _clientId,
            Username = email,
            ConfirmationCode = confirmationCode
        }, cancellationToken);
    }
}
