using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Services.Plugins;

namespace NovelsCollector.Core.Controllers
{
    [ApiController]
    [Tags("03. Category")]
    [Route("api/v1")]
    public class CategoryControler : ControllerBase
    {
        #region Injected Services
        private readonly ILogger<CategoryControler> _logger;
        private readonly SourcePluginsManager _sourcesPlugins;

        public CategoryControler(ILogger<CategoryControler> logger, SourcePluginsManager sourcePluginManager)
        {
            _logger = logger;
            _sourcesPlugins = sourcePluginManager;
        }
        #endregion

        [HttpGet("categories")]
        [EndpointSummary("Get all categories from a source")]
        public async Task<IActionResult> Get([FromQuery] string source)
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

        [HttpGet("category")]
        [EndpointSummary("Get a category from a source")]
        public async Task<IActionResult> Get([FromQuery] string source, [FromQuery] string categorySlug, [FromQuery] int page = 1)
        {
            try
            {
                var (category, totalPage) = await _sourcesPlugins.GetNovelsByCategory(source, categorySlug, page);
                return Ok(new
                {
                    data = category,
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



    }
}
