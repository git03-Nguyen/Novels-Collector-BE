using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Plugins;

namespace NovelsCollector.Core.Controllers
{
    [Route("api/v1/sources")]
    [ApiController]
    public class SourcesController : ControllerBase
    {
        private readonly ILogger<SourcesController> _logger;

        public SourcesController(ILogger<SourcesController> logger)
        {
            _logger = logger;
        }

        // GET: api/v1/sources: a list of all source plugins
        [HttpGet]
        public IActionResult Get([FromServices] IPluginManager pluginManager)
        {
            return Ok(pluginManager.Plugins);
        }

        // GET: api/v1/sources/reload: reload source plugins
        [HttpGet("reload")]
        public IActionResult Reload([FromServices] IPluginManager pluginManager)
        {
            pluginManager.LoadPlugins();
            return Ok(pluginManager.Plugins);
        }

    }
}
