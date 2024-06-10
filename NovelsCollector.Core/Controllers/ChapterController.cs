using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Services.Plugins;
using NovelsCollector.SDK.Models;

namespace NovelsCollector.Core.Controllers
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
            try
            {
                var chapter = await _sourcePluginManager.GetChapter(source, novelSlug, chapterSlug);

                // Check if the chapter is not found
                if (chapter == null) return NotFound(new { error = new { message = "Chapter not found" } });

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
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = ex.HResult, message = ex.Message } });
            }
        }

        [HttpPost("{source}/{novelSlug}/{chapterNumber}/others")]
        [EndpointSummary("Find this chapter in other sources")]
        async public Task<IActionResult> GetOtherSources([FromRoute] string source, [FromRoute] string novelSlug, [FromRoute] int chapterNumber,
            [FromBody] Dictionary<string, Novel> novelInOtherSources)
        {
            try
            {
                var currentChapter = new Chapter
                {
                    Source = source,
                    NovelSlug = novelSlug,
                    Number = chapterNumber
                };

                var chapterInOtherSources = await _sourcePluginManager.GetChapterFromOtherSources(novelInOtherSources, currentChapter);

                // Return the chapter
                return Ok(new
                {
                    data = chapterInOtherSources,
                    meta = new
                    {

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
