using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Services;

namespace NovelsCollector.Core.Controllers
{
    [ApiController]
    [Tags("03. Category")]
    [Route("api/v1/category")]
    public class CategoryController : ControllerBase
    {
        #region Injected Services
        private readonly ILogger<CategoryController> _logger;
        private readonly SourcePluginsManager _sourcesPlugins;

        public CategoryController(ILogger<CategoryController> logger, SourcePluginsManager sourcePluginManager)
        {
            _logger = logger;
            _sourcesPlugins = sourcePluginManager;
        }
        #endregion

        /// <summary>
        /// Get all categories from a source
        /// </summary>
        /// <param name="source"> The source name. e.g. 'TruyenFullVn' </param>
        /// <returns> A list of categories from a source </returns>
        [HttpGet("{source}")]
        [EndpointSummary("Get all categories from a source")]
        public async Task<IActionResult> Get([FromRoute] string source)
        {
            var categories = await _sourcesPlugins.GetCategories(source);

            return Ok(new
            {
                data = categories,
                meta = new { source }
            });
        }

        /// <summary>
        /// Get novels by category from a source
        /// </summary>
        /// <param name="source"> The source name. e.g. 'TruyenFullVn' </param>
        /// <param name="categorySlug"> The category slug. e.g. 'ngon-tinh' </param>
        /// <param name="page"> The page number. Default is 1 </param>
        /// <returns></returns>
        [HttpGet("{source}/{categorySlug}")]
        [EndpointSummary("Get novels by category from a source")]
        public async Task<IActionResult> Get([FromRoute] string source, [FromRoute] string categorySlug, [FromQuery] int page = 1)
        {
            var (novels, totalPage) = await _sourcesPlugins.GetNovelsByCategory(source, categorySlug, page);

            return Ok(new
            {
                data = novels,
                meta = new
                {
                    source,
                    categorySlug,
                    page,
                    totalPage
                }
            });
        }

        /// <summary>
        /// Get hot novels from a source
        /// </summary>
        /// <param name="source"> The source name. e.g. 'TruyenFullVn' </param>
        /// <param name="page"> The page number. Default is 1 </param>
        /// <returns> A list of hot novels from a source </returns>
        [HttpGet("{source}/hot")]
        [EndpointSummary("Get hot novels from a source")]
        public async Task<IActionResult> GetHot([FromRoute] string source, [FromQuery] int page = 1)
        {
            var (novels, totalPage) = await _sourcesPlugins.GetHotNovels(source, page);

            return Ok(new
            {
                data = novels,
                meta = new
                {
                    source,
                    page,
                    totalPage
                }
            });
        }

        /// <summary>
        /// Get latest novels from a source
        /// </summary>
        /// <param name="source"> The source name. e.g. 'TruyenFullVn' </param>
        /// <param name="page"> The page number. Default is 1 </param>
        /// <returns> A list of latest novels from a source </returns>
        [HttpGet("{source}/latest")]
        [EndpointSummary("Get latest novels from a source")]
        public async Task<IActionResult> GetLatest([FromRoute] string source, [FromQuery] int page = 1)
        {
            var (novels, totalPage) = await _sourcesPlugins.GetLatestNovels(source, page);

            return Ok(new
            {
                data = novels,
                meta = new
                {
                    source,
                    page,
                    totalPage
                }
            });
        }

        /// <summary>
        /// Get completed novels from a source
        /// </summary>
        /// <param name="source"> The source name. e.g. 'TruyenFullVn' </param>
        /// <param name="page"> The page number. Default is 1 </param>
        /// <returns> A list of completed novels from a source </returns>
        [HttpGet("{source}/completed")]
        [EndpointSummary("Get completed novels from a source")]
        public async Task<IActionResult> GetCompleted([FromRoute] string source, [FromQuery] int page = 1)
        {
            var (novels, totalPage) = await _sourcesPlugins.GetCompletedNovels(source, page);

            return Ok(new
            {
                data = novels,
                meta = new
                {
                    source,
                    page,
                    totalPage
                }
            });
        }


    }
}
