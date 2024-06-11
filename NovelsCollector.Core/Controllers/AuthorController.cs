using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Services;

namespace NovelsCollector.Core.Controllers
{
    [ApiController]
    [Tags("04. Author")]
    [Route("api/v1/author")]
    public class AuthorController : ControllerBase
    {
        #region Injected Services
        private readonly ILogger<AuthorController> _logger;
        private readonly SourcePluginsManager _sourcesPlugins;

        public AuthorController(ILogger<AuthorController> logger, SourcePluginsManager sourcePluginManager)
        {
            _logger = logger;
            _sourcesPlugins = sourcePluginManager;
        }
        #endregion

        /// <summary>
        /// Get novels by author from a source
        /// </summary>
        /// <param name="source"> The source name. e.g. 'TruyenFullVn' </param>
        /// <param name="authorSlug"> The author slug. e.g. 'tieu-tinh' </param>
        /// <param name="page"> The page number. Default is 1 </param>
        /// <returns> A list of novels by author from a source </returns>
        [HttpGet("{source}/{authorSlug}")]
        [EndpointSummary("Get novels by author from a source")]
        public async Task<IActionResult> Get([FromRoute] string source, [FromRoute] string authorSlug, [FromQuery] int page = 1)
        {
            var (novels, totalPage) = await _sourcesPlugins.GetNovelsByAuthor(source, authorSlug, page);

            return Ok(new
            {
                data = novels,
                meta = new
                {
                    source,
                    authorSlug,
                    page,
                    totalPage
                }
            });
        }
    }
}
