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

        [HttpGet("{source}/{novelSlug}/chapters")]
        [EndpointSummary("View chapters of a novel by page, -1 is last page")]
        public async Task<IActionResult> GetChapters([FromRoute] string source, [FromRoute] string novelSlug, [FromQuery] int page = -1)
        {
            if (page == 0) return BadRequest(new { error = new { message = "Page must be greater than 0" } });
            try
            {
                var (chapters, totalPage) = await _sourcePluginManager.GetChapters(source, novelSlug, page);

                // Check if the novel is not found
                if (chapters == null)
                {
                    return NotFound(new { error = new { message = "Không tìm thấy truyện" } });
                }

                // Return the chapters
                return Ok(new
                {
                    data = chapters,
                    meta = new
                    {
                        source = source,
                        novelSlug = novelSlug,
                        page = page != -1 ? page : totalPage,
                        totalPage = totalPage
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
