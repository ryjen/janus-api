using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;

namespace Janus;

using RequestParams = Dictionary<string, string>;

public partial class Auth
{
    protected AdminRespondToAuthChallengeRequest RequestChallengeResponse(RequestParams request, ChallengeNameType type)
    {
        string account = request["account"];
        string password = request["password"];
        string session = request["session"];

        return new AdminRespondToAuthChallengeRequest
        {
            ChallengeName = type,
            ClientId = clientId,
            UserPoolId = userPoolId,
            ChallengeResponses = new Dictionary<string, string> {
                {"USERNAME", account},
                {"PASSWORD", password}
            },
            Session = session
        };
    }
    protected AdminInitiateAuthRequest RequestAuth(RequestParams request)
    {
        string account = request["account"];
        string password = request["password"];

        return new AdminInitiateAuthRequest
        {
            UserPoolId = userPoolId,
            ClientId = clientId,
            AuthFlow = AuthFlowType.ADMIN_USER_PASSWORD_AUTH,
            AuthParameters = new Dictionary<string, string>
            {
                {"USERNAME", account},
                {"PASSWORD", password}
            }
        };
    }
}
