using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Services;

namespace NovelsCollector.Core.Controllers.Plugins
{
    [Authorize(Roles = "Quản trị viên")]
    [ApiController]
    [Tags("08. Exporters")]
    [Route("api/v1/exporter")]
    public class ExporterController : ControllerBase
    {
        #region Injected Services
        private readonly ILogger<ExporterController> _logger;
        private readonly ExporterPluginsManager _pluginsManager;

        public ExporterController(ILogger<ExporterController> logger, ExporterPluginsManager exporterPluginManager)
        {
            _logger = logger;
            _pluginsManager = exporterPluginManager;
        }
        #endregion

        /// <summary>
        /// Get a list of all exporter plugins.
        /// </summary>
        /// <returns> A list of all exporter plugins. </returns>
        [AllowAnonymous]
        [HttpGet]
        [EndpointSummary("Get a list of all exporter plugins")]
        public IActionResult GetExporters()
        {
            return Ok(new
            {
                data = _pluginsManager.Installed.ToArray()
            });
        }

        /// <summary>
        /// Load an exporter plugin by name.
        /// </summary>
        /// <param name="pluginName"> The name of the exporter plugin to load. </param>
        /// <returns> List of loaded exporter plugins and the just loaded plugin. </returns>
        [HttpGet("load/{pluginName}")]
        [EndpointSummary("Load an exporter plugin by name")]
        public IActionResult Load([FromRoute] string pluginName)
        {
            _pluginsManager.LoadPlugin(pluginName);
            return Ok(new
            {
                data = _pluginsManager.Installed.ToArray(),
                meta = new { loaded = pluginName }
            });
        }

        /// <summary>
        /// Unload an exporter plugin by name.
        /// </summary>
        /// <param name="pluginName"> The name of the exporter plugin to unload. </param>
        /// <returns> The list of loaded exporter plugins and the just unloaded plugin. </returns>
        [HttpGet("unload/{pluginName}")]
        [EndpointSummary("Unload an exporter plugin by name")]
        public IActionResult Unload([FromRoute] string pluginName)
        {
            _pluginsManager.UnloadPlugin(pluginName);
            return Ok(new
            {
                data = _pluginsManager.Installed.ToArray(),
                meta = new { unloaded = pluginName }
            });
        }

        /// <summary>
        /// Add a new exporter plugin from a file.
        /// </summary>
        /// <param name="downloadUrl"> The download URL of the exporter plugin. </param>
        /// <returns> The list of loaded exporter plugins and the just added plugin. </returns>
        [HttpPost("add")]
        [EndpointSummary("Add a new exporter plugin")]
        public async Task<IActionResult> Add(IFormFile file)
        {
            var added = await _pluginsManager.AddPluginFromFile(file);

            return Ok(new
            {
                data = _pluginsManager.Installed.ToArray(),
                meta = new { added }
            });
        }

        /// <summary>
        /// Remove an exporter plugin out of the disk/database by name.
        /// </summary>
        /// <param name="pluginName"> The name of the exporter plugin to remove. </param>
        /// <returns> The list of loaded exporter plugins and the just removed plugin. </returns>
        [HttpDelete("delete/{pluginName}")]
        [EndpointSummary("Remove an exporter plugin out of the disk by name")]
        public IActionResult Delete([FromRoute] string pluginName)
        {
            _pluginsManager.RemovePlugin(pluginName);
            return Ok(new
            {
                data = _pluginsManager.Installed.ToArray(),
                meta = new { removed = pluginName }
            });
        }

        /// <summary>
        /// Call the GC.Collect and to see if the plugin contexts are unloaded.
        /// </summary>
        /// <returns> The list of plugin contexts that are unloaded in the past and the current status of them (Alive/Dead). </returns>
        [HttpGet("unload/history")]
        [EndpointSummary("Call the GC.Collect and to see if the plugin contexts are unloaded successfully or not")]
        public IActionResult DebugUnloading()
        {
            for (int i = 0; i < 2; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            return Ok(new
            {
                data = _pluginsManager.unloadedHistory.Select(wr => wr.IsAlive ? "Alive" : "Dead").ToArray(),
            });
        }

    }
}
