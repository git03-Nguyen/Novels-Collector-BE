using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Services.Plugins;

namespace NovelsCollector.Core.Controllers
{
    [ApiController]
    [Tags("04. Novel Sources")]
    [Route("api/v1/sources")]
    public class SourcesController : ControllerBase
    {
        #region Injected Services
        private readonly ILogger<SourcesController> _logger;
        private readonly SourcePluginsManager _sourcePluginManager;

        public SourcesController(ILogger<SourcesController> logger, SourcePluginsManager sourcePluginManager)
        {
            _logger = logger;
            _sourcePluginManager = sourcePluginManager;
        }
        #endregion

        // GET: api/v1/sources
        [HttpGet]
        [EndpointSummary("Get a list of all source plugins")]
        public IActionResult GetSources()
        {
            return Ok(new
            {
                data = _sourcePluginManager.Plugins.Values.ToArray(),
            });
        }

        // GET: api/v1/sources/reload
        [HttpGet("reload")]
        [EndpointSummary("Reload source plugins")]
        public IActionResult Reload()
        {
            try
            {
                _sourcePluginManager.Reload();
                return Ok(new
                {
                    data = _sourcePluginManager.Plugins.Values.ToArray(),
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
            //SourcePlugin plugin = _sourcePluginManager.Plugins["PluginCrawlTruyenFull"];
            //_sourcePluginManager.Add(plugin);
            //return Ok(new
            //{
            //    data = plugin,
            //});
            throw new NotImplementedException();
        }

        // DELETE: api/v1/sources/{name}: remove a source plugin
        [HttpDelete("{name}")]
        [EndpointSummary("Remove a source plugin")]
        public IActionResult Delete([FromRoute] string name)
        {
            try
            {
                throw new NotImplementedException();
                //_sourcePluginManager.RemovePlugin(name);
                return Ok(new
                {
                    data = _sourcePluginManager.Plugins.ToArray(),
                    meta = new { removed = name }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = ex.HResult, message = ex.Message } });
            }
        }


    }
}
