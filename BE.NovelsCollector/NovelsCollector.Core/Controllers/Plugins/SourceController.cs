using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NovelsCollector.Core.Services;

namespace NovelsCollector.Core.Controllers.Plugins
{
    [Authorize(Roles = "Quản trị viên")]
    [ApiController]
    [Tags("07. Sources")]
    [Route("api/v1/source")]
    public class SourceController : ControllerBase
    {
        #region Injected Services
        private readonly ILogger<SourceController> _logger;
        private readonly SourcePluginsManager _pluginsManager;
        private readonly IMemoryCache _cacheService;

        public SourceController(ILogger<SourceController> logger, SourcePluginsManager sourcePluginManager, IMemoryCache cacheService)
        {
            _logger = logger;
            _pluginsManager = sourcePluginManager;
            _cacheService = cacheService;
        }
        #endregion

        /// <summary>
        /// Get a list of all source plugins.
        /// </summary>
        /// <returns> A list of all source plugins. </returns>
        [AllowAnonymous]
        [HttpGet]
        [EndpointSummary("Get a list of all source plugins")]
        public IActionResult Getsources()
        {
            return Ok(new
            {
                data = _pluginsManager.Installed.ToArray()
            });
        }

        /// <summary>
        /// Load a source plugin by name.
        /// </summary>
        /// <param name="pluginName"> The name of the source plugin to load. </param>
        /// <returns> List of loaded source plugins and the just loaded plugin. </returns>
        [HttpGet("load/{pluginName}")]
        [EndpointSummary("Load a source plugin by name")]
        public IActionResult Load([FromRoute] string pluginName)
        {
            _pluginsManager.LoadPlugin(pluginName);
            if (_cacheService is MemoryCache cache) cache.Clear();

            return Ok(new
            {
                data = _pluginsManager.Installed.ToArray(),
                meta = new { loaded = pluginName }
            });
        }

        /// <summary>
        /// Unload a source plugin by name.
        /// </summary>
        /// <param name="pluginName"> The name of the source plugin to unload. </param>
        /// <returns> The list of loaded source plugins and the just unloaded plugin. </returns>
        [HttpGet("unload/{pluginName}")]
        [EndpointSummary("Unload a source plugin by name")]
        public IActionResult Unload([FromRoute] string pluginName)
        {
            _pluginsManager.UnloadPlugin(pluginName);
            if (_cacheService is MemoryCache cache) cache.Clear();

            return Ok(new
            {
                data = _pluginsManager.Installed.ToArray(),
                meta = new { unloaded = pluginName }
            });
        }

        /// <summary>
        /// Add a new source plugin from a file.
        /// </summary>
        /// <param name="downloadUrl"> The download URL of the source plugin. </param>
        /// <returns> The list of loaded source plugins and the just added plugin. </returns>
        [HttpPost("add")]
        [EndpointSummary("Add a new source plugin")]
        public async Task<IActionResult> Post(IFormFile file)
        {
            var added = await _pluginsManager.AddPluginFromFile(file);
            if (_cacheService is MemoryCache cache) cache.Clear();

            return Ok(new
            {
                data = _pluginsManager.Installed.ToArray(),
                meta = new { added }
            });
        }

        /// <summary>
        /// Remove a source plugin out of the disk/database by name.
        /// </summary>
        /// <param name="pluginName"> The name of the source plugin to remove. </param>
        /// <returns> The list of loaded source plugins and the just removed plugin. </returns>
        [HttpDelete("delete/{pluginName}")]
        [EndpointSummary("Remove a source plugin out of the disk by name")]
        public IActionResult Delete([FromRoute] string pluginName)
        {
            _pluginsManager.RemovePlugin(pluginName);
            if (_cacheService is MemoryCache cache) cache.Clear();

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
        [AllowAnonymous]
        [HttpGet("unload/history")]
        [EndpointSummary("Call the GC.Collect and to see if the plugin contexts are unloaded successfully or not")]
        public IActionResult DebugUnloading()
        {
            // Disconnect the HTTP connection
            Response.Body.Close();

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
