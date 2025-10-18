using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;

namespace IntegrationApi.Services
{
    public interface ISqsService
    {
        Task SendMessageAsync<T>(T message);
    }

    public class SqsService : ISqsService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly string _queueUrl;
        private readonly ILogger<SqsService> _logger;

        public SqsService(IAmazonSQS sqsClient, IConfiguration configuration, ILogger<SqsService> logger)
        {
            _sqsClient = sqsClient;
            _queueUrl = configuration["AWS:SQS:QueueUrl"]
                ?? throw new ArgumentNullException("AWS:SQS:QueueUrl configuration is missing");
            _logger = logger;
        }

        public async Task SendMessageAsync<T>(T message)
        {
            try
            {
                var messageBody = JsonSerializer.Serialize(message, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var sendRequest = new SendMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MessageBody = messageBody
                };

                _logger.LogInformation($"Sending message to SQS: {messageBody}");

                var response = await _sqsClient.SendMessageAsync(sendRequest);

                _logger.LogInformation($"Message sent successfully. MessageId: {response.MessageId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending message to SQS: {ex.Message}");
                throw;
            }
        }
    }
}