using Amazon.DynamoDBv2.DataModel;

namespace Janus.Model
{
  [DynamoDBTable("Account")]
  public class Account
  {
	[DynamoDBHashKey] //Partition key
	public string Id
	{
	  get;
	  set;
	}
	[DynamoDBProperty]
	public string Email
	{
	  get;
	  set;
	}
	[DynamoDBProperty]
	public string Phone
	{
	  get;
	  set;
	}
	// Multi-valued (set type) attribute.
	[DynamoDBProperty("Subscriptions")]
	public List<string> Subscriptions
	{
	  get;
	  set;
	}
  }
}
