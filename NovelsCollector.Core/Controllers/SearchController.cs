using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Services.Plugins;

namespace NovelsCollector.Core.Controllers
{
    [ApiController]
    [Tags("02. Search")]
    [Route("api/v1/search")]
    public class SearchController : ControllerBase
    {
        #region Injected Services
        private readonly ILogger<SearchController> _logger;
        private readonly SourcePluginsManager _sourcesPlugins;

        public SearchController(ILogger<SearchController> logger, SourcePluginsManager sourcePluginManager)
        {
            _logger = logger;
            _sourcesPlugins = sourcePluginManager;
        }
        #endregion

        /// <summary>
        /// Search novels by source, keyword, author, year and page queries
        /// </summary>
        /// <param name="source"> The source of the novel (e.g., DTruyenCom, SSTruyenVn). </param>
        /// <param name="keyword"> The keyword to search for. </param>
        /// <param name="title" > The title keyword of the novel. </param>
        /// <param name="author"> The author keyword of the novel. </param>
        /// <param name="page"> The page number to search for. </param>
        /// <returns> An IActionResult containing the search results or an error message. </returns>
        [HttpGet("{source}")]
        [EndpointSummary("Search novels by source, keyword, name, author and page queries")]
        public async Task<IActionResult> Get(
            [FromRoute] string source,
            [FromQuery] string? keyword, [FromQuery] string? title, [FromQuery] string? author,
            [FromQuery] int page = 1)
        {
            try
            {
                var (novels, totalPage) = await _sourcesPlugins.Search(source, keyword, title, author, page);
                return Ok(new
                {
                    data = novels,
                    meta = new
                    {
                        source,
                        page,
                        totalPage,
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
