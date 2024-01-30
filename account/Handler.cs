
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Janus;

public partial class Account : ModelHandler
{
    public Account() : base("Account") { }
}
