using Microsoft.AspNetCore.Mvc;

namespace NovelsCollector.Core.Controllers
{
    [ApiController]
    [Route("/api/v1/")]
    public class HomeController : ControllerBase
    {

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // GET, POST, PUT, DELETE: /api/v1/ => return a json object: { "message": "Hello World", "method": "GET" }
        [AcceptVerbs("GET", "POST", "PUT", "DELETE")]
        public IActionResult Index()
        {
            return Ok(new { message = "Hello World", method = HttpContext.Request.Method });
        }
    }
}
