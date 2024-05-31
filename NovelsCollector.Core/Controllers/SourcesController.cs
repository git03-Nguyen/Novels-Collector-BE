using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Services.Plugins.Sources;

namespace NovelsCollector.Core.Controllers
{
    [ApiController]
    [Tags("04. Novel Sources")]
    [Route("api/v1/sources")]
    public class SourcesController : ControllerBase
    {
        private readonly ILogger<SourcesController> _logger;
        private readonly ISourcePluginManager _sourcePluginManager;

        public SourcesController(ILogger<SourcesController> logger, ISourcePluginManager sourcePluginManager)
        {
            _logger = logger;
            _sourcePluginManager = sourcePluginManager;
        }

        // GET: api/v1/sources
        [HttpGet]
        [EndpointSummary("Get a list of all source plugins")]
        public IActionResult GetSources()
        {
            return Ok(new
            {
                data = _sourcePluginManager.Plugins.Values.ToArray(),
                meta = new { count = _sourcePluginManager.Plugins.Count }
            });
        }

        // GET: api/v1/sources/reload
        [HttpGet("reload")]
        [EndpointSummary("Reload source plugins")]
        public IActionResult Reload()
        {
            try
            {
                _sourcePluginManager.ReloadPlugins();
                return Ok(new
                {
                    data = _sourcePluginManager.Plugins.Values.ToArray(),
                    meta = new { count = _sourcePluginManager.Plugins.Count }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = ex.HResult, message = ex.Message } });
            }
        }

        // POST: api/v1/sources: add a new source plugin, which is a zip file
        [HttpPost]
        [EndpointSummary("Add a new source plugin")]
        //public async Task<IActionResult> Post([FromServices] ISourcePluginManager pluginManager, [FromForm] IFormFile file)
        public async Task<IActionResult> Post([FromBody] string file)
        {
            //if (file == null)
            //    return BadRequest(new { message = "No file uploaded" });

            //try
            //{
            //    await pluginManager.AddPluginAsync(file);
            //    return Ok(pluginManager.Plugins);
            //}
            //catch (Exception ex)
            //{
            //    return StatusCode(500, new { message = ex.Message });
            //}
            return BadRequest(new { error = new { code = 501, message = "Not implemented yet" } });
        }

        // DELETE: api/v1/sources/{name}: remove a source plugin
        [HttpDelete("{name}")]
        [EndpointSummary("Remove a source plugin")]
        public IActionResult Delete([FromRoute] string name)
        {
            try
            {
                _sourcePluginManager.RemovePlugin(name);
                return Ok(new
                {
                    data = _sourcePluginManager.Plugins.ToArray(),
                    meta = new { count = _sourcePluginManager.Plugins.Count, message = $"Plugin {name} removed" }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = ex.HResult, message = ex.Message } });
            }
        }


    }
}
