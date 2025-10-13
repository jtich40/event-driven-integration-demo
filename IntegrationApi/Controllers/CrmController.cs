using Microsoft.AspNetCore.Mvc;
using IntegrationApi.Models;

namespace IntegrationApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CrmController : ControllerBase
    {
        private readonly ILogger<CrmController> _logger;
        private readonly HttpClient _httpClient;

        // in-memory list for now, will replace with DynamoDB later
        private static List<User> _users = new List<User>();

        public CrmController(ILogger<CrmController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        // GET api/crm
        [HttpGet]
        public ActionResult<IEnumerable<User>> GetUsers()
        {
            return Ok(_users);
        }

        // GET: api/crm/{id}
        [HttpGet("{id}")]
        public ActionResult<User> GetUser(string id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
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
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"External integration failed: {ex.Message}");
            }

            // Generate ID and save user
            user.Id = Guid.NewGuid().ToString();
            _users.Add(user);

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
    }
}
