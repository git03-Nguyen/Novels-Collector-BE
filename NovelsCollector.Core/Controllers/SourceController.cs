using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Services;

namespace NovelsCollector.Core.Controllers
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
        /// <returns> An IActionResult containing the list of source plugins or an error message. </returns>
        [HttpGet]
        [EndpointSummary("Get a list of all source plugins")]
        public IActionResult Get()
        {
            return Ok(new
            {
                data = _sourcesPlugins.Plugins.Values.ToArray(),
                // TODO: add the unloaded/disabled plugins
            });
        }

        /// <summary>
        /// Reload all enabled source plugins
        /// </summary>
        /// <returns> An IActionResult containing the reloaded source plugins or an error message. </returns>
        [HttpGet("reload")]
        [EndpointSummary("Reload source plugins")]
        public IActionResult Reload()
        {
            try
            {
                _sourcesPlugins.ReloadPlugins();
                return Ok(new
                {
                    data = _sourcesPlugins.Plugins.Values.ToArray(),
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = ex.HResult, message = ex.Message } });
            }
        }

        /// <summary>
        /// Unload all source plugins
        /// </summary>
        /// <returns> An IActionResult containing the being-loaded source plugins or an error message. </returns>
        [HttpGet("unload/all")]
        [EndpointSummary("Unload all source plugins")]
        public IActionResult Unload()
        {
            try
            {
                _sourcesPlugins.UnloadAll();
                return Ok(new
                {
                    data = _sourcesPlugins.Plugins.Values.ToArray(),
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = ex.HResult, message = ex.Message } });
            }
        }

        /// <summary>
        /// Unload a source plugin by name
        /// </summary>
        /// <param name="pluginName"> The name of the source plugin to unload. </param>
        /// <returns> An IActionResult containing the list of loaded source plugins or an error message. </returns>
        [HttpGet("unload/{pluginName}")]
        [EndpointSummary("Unload a source plugin by name")]
        public IActionResult Unload([FromRoute] string pluginName)
        {
            try
            {
                _sourcesPlugins.UnloadPlugin(pluginName);
                return Ok(new
                {
                    data = _sourcesPlugins.Plugins.Values.ToArray(),
                    meta = new { unloaded = pluginName }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = ex.HResult, message = ex.Message } });
            }
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
                data = _sourcesPlugins.unloadedContexts.Select(wr => wr.IsAlive ? "Alive" : "Dead").ToArray(),
            });
        }

        /// <summary>
        /// Add a new source plugin
        /// </summary>
        /// <param name="file"> The source plugin file to add (TODO: maybe .zip). </param>
        /// <returns> An IActionResult containing the information of just added source plugin or an error message. </returns>
        [HttpPost("add")]
        [EndpointSummary("Add a new source plugin")]
        public async Task<IActionResult> Post([FromBody] string file)
        {
            try
            {
                return Ok(new
                {
                    // TODO: implement
                    data = new { },
                    meta = new { added = file }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = ex.HResult, message = ex.Message } });
            }
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
            try
            {
                // TODO: implement
                return Ok(new
                {
                    data = _sourcesPlugins.Plugins.Values.ToArray(),
                    meta = new { removed = pluginName }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = ex.HResult, message = ex.Message } });
            }
        }


    }
}
