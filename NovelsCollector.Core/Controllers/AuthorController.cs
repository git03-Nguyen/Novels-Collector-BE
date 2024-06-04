using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Services.Plugins;

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

        [HttpGet("{source}/{authorSlug}")]
        [EndpointSummary("Get novels by author from a source")]
        public async Task<IActionResult> Get([FromRoute] string source, [FromRoute] string authorSlug, [FromQuery] int page = 1)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = ex.HResult, message = ex.Message } });
            }
        }
    }
}
