using Xunit;
using Moq;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.SQSEvents;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ErpProcessorLambda.Models;
using System.Text.Json;

namespace ErpProcessorLambda.Tests
{
    public class FunctionTests
    {
        [Fact]
        public async Task SqsFunctionHandler_ProcessesValidMessage_Successfully()
        {
            // Arrange
            var mockDynamoDb = new Mock<IAmazonDynamoDB>();
            mockDynamoDb.Setup(x => x.PutItemAsync(It.IsAny<PutItemRequest>(), default))
                .ReturnsAsync(new PutItemResponse());

            var function = new Function(mockDynamoDb.Object);
            var context = new TestLambdaContext();

            var userEvent = new UserCreatedEvent
            {
                EventId = "test-event-123",
                EventType = "UserCreated",
                Timestamp = DateTime.UtcNow,
                User = new UserData
                {
                    Id = "user-123",
                    Name = "Test User",
                    Email = "test@example.com"
                }
            };

            var sqsEvent = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    new SQSEvent.SQSMessage
                    {
                        MessageId = "msg-123",
                        Body = JsonSerializer.Serialize(userEvent, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        })
                    }
                }
            };

            // Act
            await function.SqsFunctionHandler(sqsEvent, context);

            // Assert
            mockDynamoDb.Verify(x => x.PutItemAsync(
                It.Is<PutItemRequest>(r => r.TableName == "ErpProcessedUsers"),
                default
            ), Times.Once);
        }

        [Fact]
        public async Task SqsFunctionHandler_WithMultipleMessages_ProcessesAll()
        {
            // Arrange
            var mockDynamoDb = new Mock<IAmazonDynamoDB>();
            mockDynamoDb.Setup(x => x.PutItemAsync(It.IsAny<PutItemRequest>(), default))
                .ReturnsAsync(new PutItemResponse());

            var function = new Function(mockDynamoDb.Object);
            var context = new TestLambdaContext();

            var sqsEvent = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    CreateTestMessage("evt-1", "user-1"),
                    CreateTestMessage("evt-2", "user-2"),
                    CreateTestMessage("evt-3", "user-3")
                }
            };

            // Act
            await function.SqsFunctionHandler(sqsEvent, context);

            // Assert
            mockDynamoDb.Verify(x => x.PutItemAsync(It.IsAny<PutItemRequest>(), default), Times.Exactly(3));
        }

        private SQSEvent.SQSMessage CreateTestMessage(string eventId, string userId)
        {
            var userEvent = new UserCreatedEvent
            {
                eventId = eventId,
                EventType = "UserCreated",
                Timestamp = DateTime.UtcNow,
                User = new UserData
                {
                    Id = userId,
                    Name = $"User {userId}",
                    Email = $"{userId}@example.com"
                }
            };

            return new SQSEvent.SQSMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                Body = JsonSerializer.Serialize(userEvent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })
            };
        }
    }
}