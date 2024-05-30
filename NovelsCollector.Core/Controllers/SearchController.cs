using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.PluginsManager;

namespace NovelsCollector.Core.Controllers
{
    [Route("api/v1/search")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ILogger<SearchController> _logger;

        public SearchController(ILogger<SearchController> logger) => _logger = logger;

        // GET: api/v1/search?keyword=keyword&author=author&year=year
        [HttpGet]
        [EndpointSummary("Search novels by keyword, author, and year queries")]
        public async Task<IActionResult> Get([FromServices] ISourcePluginManager pluginManager,
            [FromQuery] string? keyword, [FromQuery] string? author, [FromQuery] string? year)
        {
            try
            {
                var result = await pluginManager.Search(keyword, author, year);
                return Ok(new 
                {
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }



    }
}
