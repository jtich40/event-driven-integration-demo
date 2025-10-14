using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using IntegrationApi.Models;

namespace IntegrationApi.Services
{
    public interface IDynamoDBService
    {
        Task<User?> GetUserAsync(string id);
        Task<List<User>> GetAllUsersAsync();
        Task SaveUserAsync(User user);
    }
    
    public class DynamoDBService : IDynamoDBService
    {
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly string _tableName = "Users";

        public DynamoDBService(IAmazonDynamoDB dynamoDb)
        {
            _dynamoDb = dynamoDb;
        }

        public async Task<User?> GetUserAsync(string id)
        {
            var request = new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "Id", new AttributeValue { S = id } }
                }
            };

            var response = await _dynamoDb.GetItemAsync(request);

            if (response.Item == null || response.Item.Count == 0)
            {
                return null;
            }

            return new User
            {
                Id = response.Item["Id"].S,
                Name = response.Item["Name"].S,
                Email = response.Item["Email"].S
            };
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var request = new ScanRequest
            {
                TableName = _tableName
            };

            var response = await _dynamoDb.ScanAsync(request);

            return response.Items.Select(item => new User
            {
                Id = item["Id"].S,
                Name = item["Name"].S,
                Email = item["Email"].S
            }).ToList();
        }
    
        public async Task SaveUserAsync(User user)
        {
            var request = new PutItemRequest
            {
                TableName = _tableName,
                Item = new Dictionary<string, AttributeValue>
                {
                    { "Id", new AttributeValue { S = user.Id } },
                    { "Name", new AttributeValue { S = user.Name } },
                    { "Email", new AttributeValue { S = user.Email } }
                }
            };

            await _dynamoDb.PutItemAsync(request);
        }
    }
}