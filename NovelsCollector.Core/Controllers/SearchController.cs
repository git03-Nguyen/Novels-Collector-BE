using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Services.Plugins.Sources;

namespace NovelsCollector.Core.Controllers
{
    [ApiController]
    [Tags("02. Search")]
    [Route("api/v1/search")]
    public class SearchController : ControllerBase
    {
        private readonly ILogger<SearchController> _logger;
        private readonly ISourcePluginManager _sourcePluginManager;

        public SearchController(ILogger<SearchController> logger, ISourcePluginManager sourcePluginManager)
        {
            _logger = logger;
            _sourcePluginManager = sourcePluginManager;
        }

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
                return StatusCode(500, new { error = new { code = ex.HResult, message = ex.Message } });
            }
        }



    }
}
