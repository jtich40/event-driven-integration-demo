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

        public CrmController(
            ILogger<CrmController> logger,
            IHttpClientFactory httpClientFactory,
            IDynamoDBService dynamoDb
        )
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _dynamoDb = dynamoDb;
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
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            // mock integration calling external API (simulating Salesforce integration)
            try
            {
                var response = await _httpClient.GetAsync("https://jsonplaceholder.typicode.com/users/1");
                response.EnsureSuccessStatusCode();
                var externalData = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Integrated with external system: {externalData.Substring(0, 50)}...");

                // generate ID and save to DynamoDB
                user.Id = Guid.NewGuid().ToString();
                await _dynamoDb.SaveUserAsync(user);

                _logger.LogInformation($"User {user.Id} saved to DynamoDB.");

                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"External integration failed: {ex.Message}");
                // continue saving even if integration fails
                user.Id = Guid.NewGuid().ToString();
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
