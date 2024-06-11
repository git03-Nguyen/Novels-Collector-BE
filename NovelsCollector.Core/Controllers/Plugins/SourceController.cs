using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Services;

namespace NovelsCollector.Core.Controllers.Plugins
{
    [ApiController]
    [Tags("07. Sources")]
    [Route("api/v1/source")]
    public class SourceController : ControllerBase
    {
        #region Injected Services
        private readonly ILogger<SourceController> _logger;
        private readonly SourcePluginsManager _sourcesPlugins;

        public SourceController(ILogger<SourceController> logger, SourcePluginsManager sourcePluginManager)
        {
            _logger = logger;
            _sourcesPlugins = sourcePluginManager;
        }
        #endregion

        /// <summary>
        /// Get a list of all source plugins being loaded in the system
        /// </summary>
        /// <returns> The list of source plugins being loaded in the system. </returns>
        [HttpGet]
        [EndpointSummary("Get a list of all source plugins")]
        public IActionResult Get()
        {
            return Ok(new
            {
                data = _sourcesPlugins.Installed.ToArray()
            });
        }

        /// <summary>
        /// Load a source plugin by name
        /// </summary>
        /// <param name="pluginName"> The name of the source plugin to load. </param>
        /// <returns> List of loaded source plugins. </returns>
        [HttpGet("load/{pluginName}")]
        [EndpointSummary("Load a source plugin by name")]
        public IActionResult Load([FromRoute] string pluginName)
        {
            _sourcesPlugins.LoadPlugin(pluginName);
            return Ok(new
            {
                data = _sourcesPlugins.Installed.ToArray(),
                meta = new { loaded = pluginName }
            });
        }

        /// <summary>
        /// Unload a source plugin by name
        /// </summary>
        /// <param name="pluginName"> The name of the source plugin to unload. </param>
        /// <returns> The list of loaded source plugins. </returns>
        [HttpGet("unload/{pluginName}")]
        [EndpointSummary("Unload a source plugin by name")]
        public IActionResult Unload([FromRoute] string pluginName)
        {
            _sourcesPlugins.UnloadPlugin(pluginName);
            return Ok(new
            {
                data = _sourcesPlugins.Installed.ToArray(),
                meta = new { unloaded = pluginName }
            });
        }

        /// <summary>
        /// Add a new source plugin
        /// </summary>
        /// <param name="downloadUrl"> The download URL of the source plugin to add (.zip file). </param>
        /// <returns> An IActionResult containing the information of just added source plugin or an error message. </returns>
        [HttpPost("add")]
        [EndpointSummary("Add a new source plugin")]
        public async Task<IActionResult> Post(IFormFile file)
        {
            var added = await _sourcesPlugins.AddPluginFromFile(file);

            return Ok(new
            {
                data = _sourcesPlugins.Installed.ToArray(),
                meta = new { added }
            });
        }

        /// <summary>
        /// Remove a source plugin out of the disk/database by name.
        /// </summary>
        /// <param name="pluginName"> The name of the source plugin to remove. </param>
        /// <returns> An IActionResult containing the list of loaded source plugins or an error message. </returns>
        [HttpDelete("delete/{pluginName}")]
        [EndpointSummary("Remove a source plugin out of the disk by name")]
        public IActionResult Delete([FromRoute] string pluginName)
        {
            _sourcesPlugins.RemovePlugin(pluginName);
            return Ok(new
            {
                data = _sourcesPlugins.Installed.ToArray(),
                meta = new { removed = pluginName }
            });
        }

        /// <summary>
        /// Call the GC.Collect and to see if the plugin contexts are unloaded
        /// </summary>
        /// <returns> An IActionResult containing the status of the history of unloaded plugin contexts (Dead or Alive). </returns>
        [HttpGet("unload/history")]
        [EndpointSummary("Call the GC.Collect and to see if the plugin contexts are unloaded successfully or not")]
        public IActionResult DebugUnloading()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            return Ok(new
            {
                data = _sourcesPlugins.unloadedHistory.Select(wr => wr.IsAlive ? "Alive" : "Dead").ToArray(),
            });
        }


    }
}
