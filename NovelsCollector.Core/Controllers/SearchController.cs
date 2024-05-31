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
        [EndpointSummary("Search novels by source, keyword, author, year and page queries")]
        public async Task<IActionResult> Get(   
            [FromQuery] string source, 
            [FromQuery] string? keyword, [FromQuery] string? author, [FromQuery] string? year, 
            [FromQuery] int page = 1)
        {
            try
            {
                var (novels, totalPage) = await _sourcePluginManager.Search(source, keyword, author, year, page);
                return Ok(new
                {
                    data = novels,
                    meta = new 
                    {
                        totalPage = totalPage,
                        source = source,
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = ex.HResult, message = ex.Message } });
            }
        }



    }
}
