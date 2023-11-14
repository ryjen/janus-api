using Amazon.Lambda.APIGatewayEvents;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Newtonsoft.Json;
using System.Net;

namespace Janus;

public class MutableAuth
{
    private readonly IAmazonDynamoDB _dynamoDbClient = new AmazonDynamoDBClient();

    public async Task<APIGatewayProxyResponse> Authenticate(APIGatewayProxyRequest request, ILambdaContext context)
    {
        string accountId = request.PathParameters["id"];

        Table table = Table.LoadTable(_dynamoDbClient, "Account");
        Document account = await table.GetItemAsync(accountId);

        if (account == null)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Body = "Authentication failed",
            };
        }

        // Authentication successful
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.OK,
            Body = JsonConvert.SerializeObject(new { Message = "Authentication successful" }),
        };
    }
}
