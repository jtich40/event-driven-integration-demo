using Microsoft.AspNetCore.Mvc;
using IntegrationApi.Models;
using IntegrationApi.Services;

namespace IntegrationApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CrmController : ControllerBase
    {
        private readonly ILogger<CrmController> _logger;
        private readonly HttpClient _httpClient;
        private readonly IDynamoDBService _dynamoDb;
        private readonly ISqsService _sqsService;

        public CrmController(
            ILogger<CrmController> logger,
            IHttpClientFactory httpClientFactory,
            IDynamoDBService dynamoDb,
            ISqsService sqsService)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _dynamoDb = dynamoDb;
            _sqsService = sqsService;
        }

        // GET api/crm
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            try
            {
                var users = await _dynamoDb.GetAllUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving users: {ex.Message}");
                return StatusCode(500, "Error retrieving users");
            }
        }

        // GET: api/crm/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(string id)
        {
            try
            {
                var user = await _dynamoDb.GetUserAsync(id);
                if (user == null)
                {
                    return NotFound();
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving user {id}: {ex.Message}");
                return StatusCode(500, "Error retrieving user");
            }
        }

        // POST: api/crm
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(CreateUserDto userDto)
        {
            // mock integration calling external API (simulating Salesforce integration)
            try
            {
                var response = await _httpClient.GetAsync("https://jsonplaceholder.typicode.com/users/1");
                response.EnsureSuccessStatusCode();
                var externalData = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Integrated with external system: {externalData.Substring(0, 50)}...");

                // map DTO to entity
                var user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = userDto.Name,
                    Email = userDto.Email
                };
                await _dynamoDb.SaveUserAsync(user);

                _logger.LogInformation($"User {user.Id} saved to DynamoDB.");

                // publish event to SQS for downstream processing
                var userEvent = new UserCreatedEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    EventType = "UserCreated",
                    Timestamp = DateTime.UtcNow,
                    User = new UserEventData
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Email = user.Email
                    }
                };

                await _sqsService.SendMessageAsync(userEvent);
                _logger.LogInformation($"Published UserCreated event to SQS: {userEvent.EventId}");

                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"External integration failed: {ex.Message}");
                // continue saving even if integration fails
                 var user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = userDto.Name,
                    Email = userDto.Email
                };
                await _dynamoDb.SaveUserAsync(user);
                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error creating user: {ex.Message}");
                return StatusCode(500, "Error creating user");
            }
        }
    }
}
