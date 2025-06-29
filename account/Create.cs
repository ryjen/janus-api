
using Amazon.Lambda.SQSEvents;
using Newtonsoft.Json;

namespace Janus;

using static Shared;
using AccountModel = Janus.Model.Account;

public partial class Account
{
  public async Task Create(SQSEvent sqs, ILambdaContext context)
  {
	log(sqs);
	try
	{
	  foreach (var record in sqs.Records)
	  {
		var account = JsonConvert.DeserializeObject<AccountModel>(record.Body);

		var request = new CreateRequest
		{
		  Entity = "account",
		  Data = account
		};
		
		await Create(request);
	  }
	}
	catch (Exception ex)
	{
	  log(ex.Message);
	  throw;
	}
  }
}
