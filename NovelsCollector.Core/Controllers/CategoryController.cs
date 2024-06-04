using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Services.Plugins;

namespace NovelsCollector.Core.Controllers
{
    [ApiController]
    [Tags("03. Category")]
    [Route("api/v1")]
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

        [HttpGet("category/{source}")]
        [EndpointSummary("Get all categories from a source")]
        public async Task<IActionResult> Get([FromRoute] string source)
        {
            try
            {
                var categories = await _sourcesPlugins.GetCategories(source);
                return Ok(new
                {
                    data = categories,
                    meta = new { source = source }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = ex.HResult, message = ex.Message } });
            }
        }

        [HttpGet("category/{source}/{categorySlug}")]
        [EndpointSummary("Get novels by category from a source")]
        public async Task<IActionResult> Get([FromRoute] string source, [FromRoute] string categorySlug, [FromQuery] int page = 1)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = ex.HResult, message = ex.Message } });
            }
        }

        [HttpGet("category/{source}/hot")]
        [EndpointSummary("Get hot novels from a source")]
        public async Task<IActionResult> GetHot([FromRoute] string source, [FromQuery] int page = 1)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = ex.HResult, message = ex.Message } });
            }
        }

        [HttpGet("category/{source}/latest")]
        [EndpointSummary("Get latest novels from a source")]
        public async Task<IActionResult> GetLatest([FromRoute] string source, [FromQuery] int page = 1)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = ex.HResult, message = ex.Message } });
            }
        }

        [HttpGet("category/{source}/completed")]
        [EndpointSummary("Get completed novels from a source")]
        public async Task<IActionResult> GetCompleted([FromRoute] string source, [FromQuery] int page = 1)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = ex.HResult, message = ex.Message } });
            }
        }


    }
}
