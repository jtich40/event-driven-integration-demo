using Xunit;
using Moq;
using Moq.Protected;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Mvc;
using IntegrationApi.Controllers;
using IntegrationApi.Models;
using IntegrationApi.Services;

namespace IntegrationApi.Tests.Controllers
{
    public class CrmControllerTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<IDynamoDbService> _mockDynamoDb;
        private readonly Mock<ISqsService> _mockSqs;
        private readonly CrmController _controller;

        public CrmControllerTests()
        {
            var logger = new NullLogger<CrmController>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockDynamoDb = new Mock<IDynamoDbService>();
            _mockSqs = new Mock<ISqsService>();

            // setup default HttpClient mock
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent("{\"id\":1,\"name\":\"Test\"}")
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object); ;
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // setup DynamoDB and SQS mocks to return successful completions
            _mockDynamoDb.Setup(x => x.SaveUserAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            _mockDynamoDb.Setup(x => x.GetAllUsersAsync())
                .ReturnsAsync(new List<User>());

            _mockDynamoDb.Setup(x => x.GetUserAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            _mockSqs.Setup(x => x.SendMessageAsync(It.IsAny<UserCreatedEvent>()))
                .Returns(Task.CompletedTask);
            
            _controller = new CrmController(
                logger,
                _mockHttpClientFactory.Object,
                _mockDynamoDb.Object,
                _mockSqs.Object
            );
        }

        [Fact]
        public async Task GetUsers_ReturnsOkResult_WithListOfUsers()
        {
            // Arrange
            var mockUsers = new List<User>
            {
                new User { Id = "1", Name = "Test User", Email = "test@example.com"}
            };
            _mockDynamoDb.Setup(x => x.GetAllUsersAsync()).ReturnsAsync(mockUsers);

            // Act
            var result = await _controller.GetUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var users = Assert.IsAssignableFrom<List<User>>(okResult.Value);
            Assert.Single(users);
        }

        [Fact]
        public async Task GetUser_WithValidId_ReturnsUser()
        {
            // Arrange
            var userId = "test-123";
            var mockUser = new User { Id = userId, Name = "Test User", Email = "test@example.com" };
            _mockDynamoDb.Setup(x => x.GetUserAsync(userId)).ReturnsAsync(mockUser);

            // Act
            var result = await _controller.GetUser(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var user = Assert.IsType<User>(okResult.Value);
            Assert.Equal(userId, user.Id);
        }

        [Fact]
        public async Task GetUser_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            _mockDynamoDb.Setup(x => x.GetUserAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            // Act
            var result = await _controller.GetUser("invalid-id");

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateUser_SavesUserAndPublishesEvent()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                Name = "New User",
                Email = "new@example.com"
            };

            // Mock HttpClient to return successful response
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    // at least 50 chars to pass logging substring
                    Content = new StringContent(new string('x', 100))
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // setup mocks to track if they're called and capture any exceptions
            _mockDynamoDb.Setup(x => x.SaveUserAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            _mockSqs.Setup(x => x.SendMessageAsync(It.IsAny<UserCreatedEvent>()))
                .Returns(Task.CompletedTask);

            // Act
             var result = await _controller.CreateUser(createUserDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result!.Result);
            var user = Assert.IsType<User>(createdResult.Value);

            Assert.NotNull(user.Id);
            Assert.Equal(createUserDto.Name, user.Name);
            Assert.Equal(createUserDto.Email, user.Email);

            _mockDynamoDb.Verify(x => x.SaveUserAsync(It.Is<User>(u =>
                u.Name == createUserDto.Name &&
                u.Email == createUserDto.Email
                )), Times.Once);

            _mockSqs.Verify(x => x.SendMessageAsync(It.Is<UserCreatedEvent>(e =>
                e.User.Name == createUserDto.Name &&
                e.User.Email == createUserDto.Email
                )), Times.Once);
        }
    }
}