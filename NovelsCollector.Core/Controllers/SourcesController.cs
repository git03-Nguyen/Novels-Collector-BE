using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.PluginsManager;

namespace NovelsCollector.Core.Controllers
{
    [Route("api/v1/sources")]
    [ApiController]
    public class SourcesController : ControllerBase
    {
        private readonly ILogger<SourcesController> _logger;

        public SourcesController(ILogger<SourcesController> logger) => _logger = logger;

        // GET: api/v1/sources
        [HttpGet]
        [EndpointSummary("Get a list of all source plugins")]
        public IActionResult Get([FromServices] ISourcePluginManager pluginManager) => Ok(pluginManager.Plugins);

        // GET: api/v1/sources/reload
        [HttpGet("reload")]
        [EndpointSummary("Reload source plugins")]
        public IActionResult Reload([FromServices] ISourcePluginManager pluginManager)
        {
            try
            {
                pluginManager.ReloadPlugins();
                return Ok(pluginManager.Plugins);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST: api/v1/sources: add a new source plugin, which is a zip file
        [HttpPost]
        [EndpointSummary("Add a new source plugin")]
        public async Task<IActionResult> Post([FromServices] ISourcePluginManager pluginManager, [FromForm] IFormFile file)
        {
            if (file == null)
                return BadRequest(new { message = "No file uploaded" });

            try
            {
                await pluginManager.AddPluginAsync(file);
                return Ok(pluginManager.Plugins);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // DELETE: api/v1/sources/{name}: remove a source plugin
        [HttpDelete("{name}")]
        [EndpointSummary("Remove a source plugin")]
        public IActionResult Delete([FromServices] ISourcePluginManager pluginManager, [FromRoute] string name)
        {
            try
            {
                pluginManager.RemovePlugin(name);
                return Ok(pluginManager.Plugins);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }


    }
}
