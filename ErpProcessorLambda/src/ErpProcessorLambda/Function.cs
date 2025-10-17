using Amazon.Lambda.Core;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ErpProcessorLambda.Models;
using Newtonsoft.Json;
using System.Reflection.Metadata;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ErpProcessorLambda;

public class Function
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName = "ErpProcessedUsers";

    // default constructor for Lambda
    public Function()
    {
        _dynamoDb = new AmazonDynamoDBClient();
    }

    // constructor for testing with DI
    public Function(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb;
    }

    /// <summary>
    /// Lambda function handler to process user created events
    /// Simulates ERP integration (e.g., Oracle Fusion, Peoplesoft)
    /// </summary>
    public async Task<string> FunctionHandler(UserCreatedEvent input, ILambdaContext context)
    {
        context.Logger.LogInformation($"Processing event: {input.EventId}");
        context.Logger.LogInformation($"User: {input.User.Name} ({input.User.Email})");

        try
        {
            // simulate ERP integration processing
            await SimulateErpIntegration(input, context);

            // store processed event in DynamoDB
            await StoreProcessedEvent(input, context);

            context.Logger.LogInformation($"Successfully processed event {input.EventId}");
            return $"Successfully processed user {input.User.Id}";
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error processing event: {ex.Message}");
            // lambda will retry on failure
            throw;
        }
    }

    private async Task SimulateErpIntegration(UserCreatedEvent userEvent, ILambdaContext context)
    {
        // simulate calling external ERP system, (Oracle Fusion, Peoplesoft)
        context.Logger.LogInformation("Simulating ERP API call...");

        // in real scenario, this would be:
        // HttpClient call to Oracle Fusion REST API
        // SOAP call to Peoplesoft
        // GraphQL to Workday

        // simulate network call
        await Task.Delay(100);

        context.Logger.LogInformation($"ERP: Created employee record for {userEvent.User.Name}");
        context.Logger.LogInformation($"ERP: Assigned employee ID: EMP-{userEvent.User.Id.Substring(0, 8)}");
    }
    
    private async Task StoreProcessedEvent(UserCreatedEvent userEvent, ILambdaContext context)
    {
        context.Logger.LogInfomration("Storing processed event in DynamoDB...");

        var request = new PutItemRequest
        {
            _tableName = _tableName,
            Item = new Dictionary<string, CustomAttributeValue>
            {
               {"EventId", new AttributeValue { S = userEvent.EventId } },
               {"UserId", new AttributeValue { S = userEvent.User.Id } },
                {"UserName", new AttributeValue { S = userEvent.User.Name } },
                {"UserEmail", new AttributeValue { S = userEvent.User.Email } },
                {"ProcessedAt", new AttributeValue { S = DateTime.UtcNow.ToString("o") } },
                {"EventType", new AttributeValue { S  = userEvent.EventType } },
                {"Status", new AttributeValue { S = "Processed" } }

            }
        };

        await _dynamoDb.PutItemAsync(request);
        context.Logger.LogInformation("Event stored successfully");
    }
}
