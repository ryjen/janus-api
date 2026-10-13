using Amazon.Lambda.APIGatewayEvents;
using static System.Net.HttpStatusCode;

namespace Janus;

using static Shared;

public sealed class AccountRecovery
{
    private readonly AccountRecoveryService _recovery;

    public AccountRecovery()
        : this(new AccountRecoveryService(new DynamoAccountStore(), new SqsAccountPublisher())) { }

    public AccountRecovery(AccountRecoveryService recovery) =>
        _recovery = recovery ?? throw new ArgumentNullException(nameof(recovery));

    public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var claims = request.RequestContext?.Authorizer?.Claims;
        if (claims == null || !claims.TryGetValue("sub", out var subject) ||
            string.IsNullOrWhiteSpace(subject))
            return Response(Unauthorized, new { Message = "Authentication required" });

        if (request.HttpMethod != "POST")
            return Response(MethodNotAllowed, new { Message = "Invalid method" });

        try
        {
            var outcome = await _recovery.RecoverAsync(claims);
            return outcome switch
            {
                AccountRecoveryOutcome.Unverified =>
                    Response(Forbidden, new { Message = "Verified email required" }),
                AccountRecoveryOutcome.AlreadyProvisioned =>
                    Response(OK, new { Message = "Account already provisioned" }),
                AccountRecoveryOutcome.Queued =>
                    Response(Accepted, new { Message = "Account provisioning queued" }),
                _ => Response(ServiceUnavailable, new { Message = "Account recovery unavailable" })
            };
        }
        catch
        {
            return Response(ServiceUnavailable, new { Message = "Account recovery unavailable" });
        }
    }
}
