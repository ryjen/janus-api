using Amazon.Lambda.APIGatewayEvents;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Janus;

using RequestParams = Dictionary<string, string>;

public partial class Auth
{
    private readonly IAmazonDynamoDB _dynamoDbClient = new AmazonDynamoDBClient();
    const string AccountTable = "Account";

    public async Task<APIGatewayProxyResponse> CreateAccount(RequestParams request)
    {
        var putRequest = new PutItemRequest
        {
            TableName = AccountTable,
        };
        var response = await _dynamoDbClient.PutItemAsync(putRequest);

        return Response(500, "Not implemented");
    }

    public async Task<APIGatewayProxyResponse> UpdateAccount(RequestParams request, ILambdaContext context)
    {
        var updateRequest = new UpdateItemRequest
        {
            TableName = AccountTable
        };
        var response = await _dynamoDbClient.UpdateItemAsync(updateRequest);

        return Response(500, "Not implemented");
    }

    public async Task<APIGatewayProxyResponse> DeleteAccount(RequestParams request, ILambdaContext context)
    {
        var deleteRequest = new DeleteItemRequest
        {
            TableName = AccountTable,
        };
        var response = await _dynamoDbClient.DeleteItemAsync(deleteRequest);
        return Response(500, "Not implemented");
    }
}
