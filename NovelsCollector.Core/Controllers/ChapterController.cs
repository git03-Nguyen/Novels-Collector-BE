using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Services.Plugins.Sources;
using NovelsCollector.SDK.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace NovelsCollector.Core.Controllers
{
    [ApiController]
    [Tags("03. Novels")]
    [Route("api/v1/novel")]
    public class ChapterController : ControllerBase
    {
        private readonly ILogger<ChapterController> _logger;
        private readonly ISourcePluginManager _sourcePluginManager;

        public ChapterController(ILogger<ChapterController> logger, ISourcePluginManager sourcePluginManager)
        {
            _logger = logger;
            _sourcePluginManager = sourcePluginManager;
        }

        [HttpGet("{source}/{slug}/{chapterSlug}")]
        [EndpointSummary("View a chapter of a novel")]
        async public Task<IActionResult> Get([FromRoute] string source, [FromRoute] string slug, [FromRoute] string chapterSlug)
        {
            Novel novel = new Novel { Sources = new string[] { source }, Slug = slug };
            try
            {
                string chapter = await _sourcePluginManager.GetChapter(novel, new Chapter { Slug = chapterSlug });
                if (chapter == null)
                    return NotFound(new { message = "Chapter not found" });
                return Ok(new
                {
                    data = new { content = chapter }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
