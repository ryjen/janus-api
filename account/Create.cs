
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
		await _db.Save(account);
	  }
	}
	catch (Exception ex)
	{
	  log(ex.Message);
	  throw;
	}
  }
}
