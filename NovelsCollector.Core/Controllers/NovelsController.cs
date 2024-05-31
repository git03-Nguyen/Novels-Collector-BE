using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Services.Plugins;

namespace NovelsCollector.Core.Controllers
{
    [ApiController]
    [Tags("03. Novels")]
    [Route("api/v1/novel")]
    public class NovelsController : ControllerBase
    {

        #region Injected Services
        private readonly ILogger<NovelsController> _logger;
        private readonly SourcePluginsManager _sourcePluginManager;

        public NovelsController(ILogger<NovelsController> logger, SourcePluginsManager sourcePluginManager)
        {
            _logger = logger;
            _sourcePluginManager = sourcePluginManager;
        }
        #endregion

        [HttpGet("{source}/{novelSlug}")]
        [EndpointSummary("View brief information of a novel")]
        public async Task<IActionResult> GetNovel([FromRoute] string source, [FromRoute] string novelSlug)
        {
            try
            {
                var novel = await _sourcePluginManager.GetNovelDetail(source, novelSlug);

                // Check if the novel is not found
                if (novel == null)
                {
                    return NotFound(new { error = new { message = "Không tìm thấy truyện" } });
                }

                // Return the novel
                return Ok(new
                {
                    data = novel,
                    meta = new
                    {
                        source = source,
                        //otherSources = novel.Sources.Where(s => s != source).ToArray()
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
