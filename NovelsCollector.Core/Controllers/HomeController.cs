using Microsoft.AspNetCore.Mvc;

namespace NovelsCollector.Core.Controllers
{
    [ApiController]
    [Route("/api/v1/")]
    public class HomeController : ControllerBase
    {

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger) => _logger = logger;

        [EndpointSummary("Check if the server is running")]
        [AcceptVerbs("GET", "POST", "PUT", "DELETE")]
        public IActionResult Index()
        {
            return Ok(new { message = "Hello, World", method = HttpContext.Request.Method });
        }
    }
}
