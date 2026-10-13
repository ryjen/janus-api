using Newtonsoft.Json.Linq;

namespace Janus;

// Cognito event adapter: only the verified trigger may publish an account.
// Dependency is injectable without AWS infrastructure in the test process.
public sealed class AccountConfirmed
{
    private readonly IAccountPublisher _publisher;

    public AccountConfirmed() : this(new SqsAccountPublisher()) { }

    public AccountConfirmed(IAccountPublisher publisher) =>
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));

    public async Task<JObject> Handler(JObject cognitoEvent, ILambdaContext context)
    {
        var account = VerifiedEnrollment.FromCognitoEvent(cognitoEvent);
        if (account is not null)
            await _publisher.PublishAsync(account);

        return cognitoEvent; // Required by Cognito PostConfirmation protocol.
    }
}
