using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.PluginsManager;
using NovelsCollector.SDK.Models;

namespace NovelsCollector.Core.Controllers
{
    [Route("api/v1/novel")]
    [ApiController]
    public class NovelsController : ControllerBase
    {
        private readonly ILogger<NovelsController> _logger;

        public NovelsController(ILogger<NovelsController> logger) => _logger = logger;

        // GET: api/v1/novel/{source}/{slug}
        [HttpGet("{source}/{slug}")]
        [EndpointDescription("View brief information of a novel")]
        async public Task<IActionResult> Get([FromServices] ISourcePluginManager sourcePluginManager,
            [FromRoute] string source, [FromRoute] string slug)
        {
            Novel novel = new Novel { Sources = new string[] { source }, Slug = slug };
            try
            {
                novel = await sourcePluginManager.GetNovelDetail(novel);
                if (novel == null)
                    return NotFound(new { message = "Novel not found" });
                return Ok(novel);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //// GET: api/v1/novel/{id}/chapters: view the novel chapters
        //[HttpGet("{id}/chapters")]
        //async public Task<IActionResult> GetChapters([FromServices] ISourcePluginManager sourcePluginManager, [FromRoute] string id)
        //{
        //    return BadRequest("Not implemented yet");
        //}

        // GET: api/v1/novel/{source}/{slug}/{chapterSlug}
        [HttpGet("{source}/{slug}/{chapterSlug}")]
        async public Task<IActionResult> GetChapter([FromServices] ISourcePluginManager sourcePluginManager,
            [FromRoute] string source, [FromRoute] string slug, [FromRoute] string chapterSlug)
        {
            Novel novel = new Novel { Sources = new string[] { source }, Slug = slug };
            try
            {
                string chapter = await sourcePluginManager.GetChapter(novel, new Chapter { Slug = chapterSlug });
                if (chapter == null)
                    return NotFound(new { message = "Chapter not found" });
                return Ok(new { content = chapter });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/v1/novel/{id}/export?extension=extension: export the novel to a file
        [HttpGet("{id}/export")]
        async public Task<IActionResult> Export([FromServices] ISourcePluginManager sourcePluginManager, [FromRoute] string id, [FromQuery] string extension)
        {
            return BadRequest("Not implemented yet");
        }






    }
}
