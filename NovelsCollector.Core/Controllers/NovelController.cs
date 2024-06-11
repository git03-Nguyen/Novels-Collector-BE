using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Exceptions;
using NovelsCollector.Core.Services;
using System.ComponentModel.DataAnnotations;

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
        /// <returns> Brief information of the novel. </returns>
        [HttpGet("{source}/{novelSlug}")]
        [EndpointSummary("View brief information of a novel")]
        public async Task<IActionResult> GetNovel([FromRoute] string source, [FromRoute] string novelSlug)
        {
            var novel = await _sourcesPlugins.GetNovelDetail(source, novelSlug);

            // Check if the novel is not found
            if (novel == null) throw new NotFoundException("Không tìm thấy truyện này.");

            // Find novel in other sources
            var otherSources = await _sourcesPlugins.GetNovelFromOtherSources(source, novel);

            // Return the novel
            return Ok(new
            {
                data = novel,
                meta = new
                {
                    source,
                    otherSources,
                }
            });
        }

        /// <summary>
        /// View a list of chapters of a novel by page
        /// </summary>
        /// <param name="source">The source of the novel (e.g., DTruyenCom, SSTruyenVn).</param>
        /// <param name="novelSlug">The slug identifier for the novel (e.g., tao-tac).</param>
        /// <param name="page">The page number of the chapters list (default is the last page = -1).</param>
        /// <returns> A list of chapters of a novel by page. </returns>
        [HttpGet("{source}/{novelSlug}/chapters")]
        [EndpointSummary("View chapters of a novel by page, -1 is last page")]
        public async Task<IActionResult> GetChapters([FromRoute] string source, [FromRoute] string novelSlug, [FromQuery] int page = -1)
        {
            // Check if the page is invalid
            if (page == 0) throw new BadHttpRequestException("Trang không hợp lệ.");

            // Get the chapters
            var (chapters, totalPage) = await _sourcesPlugins.GetChapters(source, novelSlug, page);

            // Check if the novel is not found
            if (chapters == null) throw new NotFoundException("Không tìm thấy chương cho truyện này.");

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







    }
}
