using Amazon.DynamoDBv2.DataModel;

namespace Janus.Model
{
    [DynamoDBTable("Player")]
    public class Player
    {
        [DynamoDBHashKey] //Partition key
        public string Id
        {
            get;
            set;
        }

        [DynamoDBProperty]
        public string AccountId
        {
            get;
            set;
        }
        [DynamoDBProperty]
        public string Location
        {
            get;
            set;
        }
    }
}
