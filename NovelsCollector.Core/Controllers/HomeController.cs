using Microsoft.AspNetCore.Mvc;

namespace NovelsCollector.Core.Controllers
{
    [ApiController]
    [Tags("01. Test")]
    [Route("/api/v1/")]
    public class HomeController : ControllerBase
    {
        #region Injected Services
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger) => _logger = logger;
        #endregion

        /// <summary>
        /// Check if the server is running
        /// </summary>
        /// <returns>An IActionResult containing a message indicating the server is running.</returns>
        [EndpointSummary("Check if the server is running")]
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                data = new
                {
                    message = "The server is running!",
                },
                meta = new
                {
                    method = "GET",
                    timestamp = DateTime.UtcNow
                }
            });
        }
    }
}
