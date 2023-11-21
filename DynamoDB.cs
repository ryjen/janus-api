using Amazon.Lambda.APIGatewayEvents;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System.Net;

namespace Janus;

public partial class Auth
{
  private readonly IAmazonDynamoDB _dynamoDbClient = new AmazonDynamoDBClient();
  const string AccountTable = "Account";

  public async Task<bool> CreateAccount(string id, string email)
  {
	var putRequest = new PutItemRequest
	{
	  TableName = AccountTable,
	  Item = new Dictionary<string, AttributeValue> {
				{"Id", new AttributeValue(id)},
				{"Email",new AttributeValue(email)}
			},
	};

	// TODO: Error checking, not sure what a good return value is
	var response = await _dynamoDbClient.PutItemAsync(putRequest);
	return response.HttpStatusCode == HttpStatusCode.OK;
  }

  // TODO: Unused
  public async Task<APIGatewayProxyResponse> UpdateAccount(string id, string email)
  {
	var updateRequest = new UpdateItemRequest
	{
	  TableName = AccountTable,
	  Key = new Dictionary<string, AttributeValue> {
				{"Id", new AttributeValue(id)}
			},
	  AttributeUpdates = new Dictionary<string, AttributeValueUpdate> {
				{"Email",new AttributeValueUpdate(new AttributeValue(email), AttributeAction.PUT)}
			},
	};
	var response = await _dynamoDbClient.UpdateItemAsync(updateRequest);
	return Response(500, "Not implemented");
  }

  // TODO: unused
  public async Task<APIGatewayProxyResponse> DeleteAccount(string id)
  {
	var deleteRequest = new DeleteItemRequest
	{
	  TableName = AccountTable,
	  Key = new Dictionary<string, AttributeValue> {
				{ "Id", new AttributeValue(id)},
			}
	};
	var response = await _dynamoDbClient.DeleteItemAsync(deleteRequest);
	return Response(500, "Not implemented");
  }
}
