using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.SQSEvents;
using ErpProcessorLambda.Models;
using System.Text.Json;


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
    /// API Gateway Lambda Proxy Integration handler
    /// Lambda function handler to process user created events
    /// Simulates ERP integration (e.g., Oracle Fusion, Peoplesoft)
    /// </summary>
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        context.Logger.LogInformation($"Received API Gateway request: {request.Path}");
        context.Logger.LogInformation($"HTTP Method: {request.HttpMethod}");

        try
        {
            // parse the incoming event from request body
            if (string.IsNullOrEmpty(request.Body))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Body = JsonSerializer.Serialize(new { error = "Request body is required" }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }

            var userEvent = JsonSerializer.Deserialize<UserCreatedEvent>(request.Body);

            if (userEvent == null || userEvent.User == null)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Body = JsonSerializer.Serialize(new { error = "Invalid event format" }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }

            context.Logger.LogInformation($"Processing event: {userEvent.EventId}");
            context.Logger.LogInformation($"User: {userEvent.User.Name} ({userEvent.User.Email})");

            // simulate ERP integration processing
            await SimulateErpIntegration(userEvent, context);

            // store processed event in DynamoDB
            await StoreProcessedEvent(userEvent, context);

            context.Logger.LogInformation($"Successfully processed event {userEvent.EventId}");

            // return success response
            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(new
                {
                    message = "Event processed successfully",
                    eventId = userEvent.EventId,
                    userId = userEvent.User.Id,
                    processedAt = DateTime.UtcNow
                }),
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Access-Control-Allow-Origin", "*" }
                }
            };
        }
        catch (JsonException ex)
        {
            context.Logger.LogError($"JSON parsing error: {ex.Message}");
            return new APIGatewayProxyResponse
            {
                StatusCode = 400,
                Body = JsonSerializer.Serialize(new { error = "Invalid JSON format", details = ex.Message }),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error processing event: {ex.Message}");
            return new APIGatewayProxyResponse
            {
                StatusCode = 500,
                Body = JsonSerializer.Serialize(new { error = "Internal server error", details = ex.Message }),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
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
        context.Logger.LogInformation($"ERP: Assigned employee ID: EMP-{userEvent.User.Id.Substring(0, Math.Min(8, userEvent.User.Id.Length))}");
    }

    private async Task StoreProcessedEvent(UserCreatedEvent userEvent, ILambdaContext context)
    {
        context.Logger.LogInformation("Storing processed event in DynamoDB...");

        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                { "EventId", new AttributeValue { S = userEvent.EventId } },
                { "UserId", new AttributeValue { S = userEvent.User.Id } },
                { "UserName", new AttributeValue { S = userEvent.User.Name } },
                { "UserEmail", new AttributeValue { S = userEvent.User.Email } },
                { "ProcessedAt", new AttributeValue { S = DateTime.UtcNow.ToString("o") } },
                { "EventType", new AttributeValue { S  = userEvent.EventType } },
                { "Status", new AttributeValue { S = "Processed" } }

            }
        };

        await _dynamoDb.PutItemAsync(request);
        context.Logger.LogInformation("Event stored successfully");
    }
    
    /// <summary>
    /// SQS Event handler - processes messages from queue
    /// This is triggered automatically when messages arrive in SQS
    /// </summary>
    public async Task SqsFunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        context.Logger.LogInformation($"Received {sqsEvent.Records.Count} messages from SQS");

        foreach (var message in sqsEvent.Records)
        {
            context.Logger.LogInformation($"Processing message: {message.MessageId}");
            context.Logger.LogInformation($"Message body: {message.Body}");

            try
            {
                // parse the event from SQS message body
                var userEvent = JsonSerializer.Deserialize<UserCreatedEvent>(message.Body);

                if (userEvent == null || userEvent.User == null)
                {
                    context.Logger.LogInformation($"Invalid message format: : {message.Body}");
                    // skip this message, process next
                    continue;
                }

                context.Logger.LogInformation($"Processing event: {userEvent.EventId}");
                context.Logger.LogInformation($"User: {userEvent.User.Name} ({userEvent.User.Email})");

                // process the event (same logic as API gateway)
                await SimulateErpIntegration(userEvent, context);
                await StoreProcessedEvent(userEvent, context);

                context.Logger.LogInformation($"Successfully processed message {message.MessageId}");
            }
            catch (JsonException ex)
            {
                context.Logger.LogError($"JSON parsing error for message {message.MessageId}: {ex.Message}");
                // message will go to DLQ if configured or be retried
                throw;
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error processing message {message.MessageId}: {ex.Message}");
                // let lambda retry
                throw;
            }
        }

        context.Logger.LogInformation($"Finished processing {sqsEvent.Records.Count} messages");
    }
}
