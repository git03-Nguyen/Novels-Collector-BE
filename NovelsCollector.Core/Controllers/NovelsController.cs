using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Services.Plugins.Sources;
using NovelsCollector.SDK.Models;

namespace NovelsCollector.Core.Controllers
{
    [ApiController]
    [Tags("03. Novels")]
    [Route("api/v1/novel")]
    public class NovelsController : ControllerBase
    {
        private readonly ILogger<NovelsController> _logger;
        private readonly ISourcePluginManager _sourcePluginManager;

        public NovelsController(ILogger<NovelsController> logger, ISourcePluginManager sourcePluginManager)
        {
            _logger = logger;
            _sourcePluginManager = sourcePluginManager;
        }

        [HttpGet("{source}/{slug}")]
        [EndpointSummary("View brief information of a novel")]
        async public Task<IActionResult> GetNovel([FromRoute] string source, [FromRoute] string slug)
        {
            Novel novel = new Novel { Sources = new string[] { source }, Slug = slug };
            try
            {
                novel = await _sourcePluginManager.GetNovelDetail(novel);
                if (novel == null)
                    return NotFound(new { message = "Novel not found" });
                return Ok(new { data = novel });
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

        

        // GET: api/v1/novel/{id}/export?extension=extension: export the novel to a file
        [HttpGet("{id}/export")]
        async public Task<IActionResult> Export([FromRoute] string id, [FromQuery] string extension)
        {
            return BadRequest("Not implemented yet");
        }






    }
}
