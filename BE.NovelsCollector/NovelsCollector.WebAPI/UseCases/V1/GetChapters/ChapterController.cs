using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Application.Exceptions;
using NovelsCollector.Core.Services;
using NovelsCollector.Domain.Resources.Chapters;
using NovelsCollector.Domain.Resources.Novels;

namespace NovelsCollector.WebAPI.UseCases.V1.GetChapters
{
    [ApiController]
    [Tags("06. Chapter")]
    [Route("api/v1/chapter")]
    public class ChapterController : ControllerBase
    {
        #region Injected Services
        private readonly ILogger<ChapterController> _logger;
        private readonly SourcePluginsManager _sourcePluginManager;

        public ChapterController(ILogger<ChapterController> logger, SourcePluginsManager sourcePluginManager)
        {
            _logger = logger;
            _sourcePluginManager = sourcePluginManager;
        }
        #endregion

        /// <summary>
        /// Read a chapter by source, novel slug and chapter slug
        /// </summary>
        /// <param name="source"> The source of the novel (e.g., DTruyenCom, SSTruyenVn). </param>
        /// <param name="novelSlug"> The slug identifier for the novel (e.g., tao-tac). </param>
        /// <param name="chapterSlug"> The slug identifier for the chapter (e.g., chuong-1). </param>
        /// <returns> An IActionResult containing the chapter content or an error message. </returns>
        [HttpGet("{source}/{novelSlug}/{chapterSlug}")]
        [EndpointSummary("Read a chapter of a novel by source, novel slug and chapter slug")]
        async public Task<IActionResult> Get([FromRoute] string source, [FromRoute] string novelSlug, [FromRoute] string chapterSlug)
        {
            // Get the chapter from the source
            var chapter = await _sourcePluginManager.GetChapterContent(source, novelSlug, chapterSlug);

            // Check if the chapter is not found
            if (chapter == null) throw new NotFoundException("Không tìm thấy chương này.");

            // Return the chapter
            return Ok(new
            {
                data = chapter,
                meta = new
                {
                    source,
                    novelSlug,
                    chapterSlug,
                }
            });
        }

        /// <summary>
        /// Get the same chapter in other sources
        /// </summary>
        /// <param name="source"> The source of the novel (e.g., DTruyenCom, SSTruyenVn). </param>
        /// <param name="novelSlug"> The slug identifier for the novel (e.g., tao-tac). </param>
        /// <param name="chapterNumber"> The number of the chapter (e.g., 1). </param>
        /// <param name="novelInOtherSources"> The list of the same novels in other sources. </param>
        /// <returns> An IActionResult containing the same chapter in other sources. </returns>
        [HttpPost("{source}/{novelSlug}/{chapterNumber}/others")]
        [EndpointSummary("Find this chapter in other sources")]
        async public Task<IActionResult> GetOtherSources([FromRoute] string source, [FromRoute] string novelSlug, [FromRoute] int chapterNumber,
            [FromBody] Dictionary<string, Novel> novelInOtherSources)
        {
            var currentChapter = new Chapter
            {
                Source = source,
                NovelSlug = novelSlug,
                Number = chapterNumber
            };

            var chapterInOtherSources = await _sourcePluginManager.GetChapterFromOtherSources(novelInOtherSources, currentChapter);

            // Return the same chapter in other sources
            return Ok(new
            {
                data = chapterInOtherSources
            });
        }
    }
}
