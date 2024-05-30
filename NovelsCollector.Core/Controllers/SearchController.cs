using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Services.Plugins.Sources;

namespace NovelsCollector.Core.Controllers
{
    [Route("api/v1/search")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ILogger<SearchController> _logger;
        private readonly ISourcePluginManager _sourcePluginManager;

        public SearchController(ILogger<SearchController> logger, ISourcePluginManager sourcePluginManager)
        {
            _logger = logger;
            _sourcePluginManager = sourcePluginManager;
        }

        // GET: api/v1/search?keyword=keyword&author=author&year=year
        [HttpGet]
        [EndpointSummary("Search novels by keyword, author, and year queries")]
        public async Task<IActionResult> Get([FromQuery] string? keyword, [FromQuery] string? author, [FromQuery] string? year)
        {
            try
            {
                var result = await _sourcePluginManager.Search(keyword, author, year);
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
