using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Services.Plugins;

namespace NovelsCollector.Core.Controllers
{
    [ApiController]
    [Tags("99. Test plugins")]
    [Route("api/v1/test")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<SourcesController> _logger;
        private readonly SourcePluginsManager _sourcePluginManager;

        public TestController(ILogger<SourcesController> logger, SourcePluginsManager sourcePluginManager)
        {
            _logger = logger;
            _sourcePluginManager = sourcePluginManager;
        }

        // GET: api/v1/test?name=pluginName
        [HttpGet]
        [EndpointSummary("Load a plugin")]
        public IActionResult TestPlugin([FromQuery] string name)
        {
            Console.WriteLine("Start load: " + name);
            _sourcePluginManager.LoadPluginIntoContext(name);
            return Ok();
        }

        // GET: api/v1/unload?name=pluginName
        [HttpGet("unload")]
        [EndpointSummary("Unload a plugin")]
        public IActionResult UnloadPlugin([FromQuery] string name)
        {
            Console.WriteLine("Start unload: " + name);
            _sourcePluginManager.UnloadPlugin(name);
            return Ok();
        }

        // GET: api/v1/test/wait
        [HttpGet("wait")]
        [EndpointSummary("Wait for 10 loops")]
        public IActionResult Wait()
        {
            _sourcePluginManager.WaitingForGC();
            return Ok();
        }

    }
}
