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
        public async Task<IActionResult> GetNovel([FromRoute] string source, [FromRoute] string slug)
        {
            try
            {
                var novel = await _sourcePluginManager.GetNovelDetail(source, slug);
                if (novel == null)
                    return NotFound(new { error = new { message = "Không tìm thấy truyện" } });
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
