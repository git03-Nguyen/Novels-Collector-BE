using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Services.Plugins;

namespace NovelsCollector.Core.Controllers
{
    [ApiController]
    [Tags("05. Novel")]
    [Route("api/v1/novel")]
    public class NovelController : ControllerBase
    {

        #region Injected Services
        private readonly ILogger<NovelController> _logger;
        private readonly SourcePluginsManager _sourcesPlugins;

        public NovelController(ILogger<NovelController> logger, SourcePluginsManager sourcePluginManager)
        {
            _logger = logger;
            _sourcesPlugins = sourcePluginManager;
        }
        #endregion

        /// <summary>
        /// View brief information of a novel
        /// </summary>
        /// <param name="source">The source of the novel (e.g., DTruyenCom, SSTruyenVn).</param>
        /// <param name="novelSlug">The slug identifier for the novel (e.g., tao-tac).</param>
        /// <returns>An IActionResult containing the novel information or an error message.</returns>
        [HttpGet("{source}/{novelSlug}")]
        [EndpointSummary("View brief information of a novel")]
        public async Task<IActionResult> GetNovel([FromRoute] string source, [FromRoute] string novelSlug)
        {
            try
            {
                var novel = await _sourcesPlugins.GetNovelDetail(source, novelSlug);

                // Check if the novel is not found
                if (novel == null) return NotFound(new { error = new { message = "Novel not found" } });

                // Return the novel
                return Ok(new
                {
                    data = novel,
                    meta = new
                    {
                        source,
                        //otherSources = novel.Sources.Where(s => s != source).ToArray()
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = ex.HResult, message = ex.Message } });
            }
        }

        /// <summary>
        /// View a list of chapters of a novel by page (default is the last page)
        /// </summary>
        /// <param name="source">The source of the novel (e.g., DTruyenCom, SSTruyenVn).</param>
        /// <param name="novelSlug">The slug identifier for the novel (e.g., tao-tac).</param>
        /// <param name="page">The page number of the chapters list (default is the last page = -1).</param>
        /// <returns>An IActionResult containing the chapters list or an error message.</returns>
        [HttpGet("{source}/{novelSlug}/chapters")]
        [EndpointSummary("View chapters of a novel by page, -1 is last page")]
        public async Task<IActionResult> GetChapters([FromRoute] string source, [FromRoute] string novelSlug, [FromQuery] int page = -1)
        {
            if (page == 0) return BadRequest(new { error = new { message = "Page must be greater than 0" } });
            try
            {
                var (chapters, totalPage) = await _sourcesPlugins.GetChapters(source, novelSlug, page);

                // Check if the novel is not found
                if (chapters == null) return NotFound(new { error = new { message = "Novel not found" } });

                // Return the chapters
                return Ok(new
                {
                    data = chapters,
                    meta = new
                    {
                        source,
                        novelSlug,
                        page = (page != -1) ? page : totalPage,
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
