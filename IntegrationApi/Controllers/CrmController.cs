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
    }
}
