using Microsoft.AspNetCore.Mvc;

namespace NovelsCollector.Core.Controllers
{
    [ApiController]
    [Tags("01. Test")]
    [Route("/api/v1/")]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger) => _logger = logger;

        [EndpointSummary("Check if the server is running")]
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new 
            { 
                message = "The server is running!", 
                data = new 
                { 
                    method = "GET",
                    serverTime = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")
                } });
        }
    }
}
