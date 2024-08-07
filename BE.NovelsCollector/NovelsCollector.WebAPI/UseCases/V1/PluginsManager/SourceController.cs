﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NovelsCollector.Application.UseCases.ManagePlugins;
using NovelsCollector.Domain.Constants;
using NovelsCollector.Domain.Entities.Plugins.Sources;
using NovelsCollector.Infrastructure.Persistence.Entities;

namespace NovelsCollector.WebAPI.UseCases.V1.PluginsManager
{
    [Authorize(Roles = Roles.Administrator)]
    [ApiController]
    [Tags("07. Sources")]
    [Route("api/v1/source")]
    public class SourceController : ControllerBase
    {
        #region Injected Services
        private readonly ILogger<SourceController> _logger;
        private readonly BasePluginsManager<SourcePlugin, ISourceFeature> _pluginsManager;
        private readonly IMemoryCache _cacheService;

        public SourceController(ILogger<SourceController> logger, BasePluginsManager<SourcePlugin, ISourceFeature> pluginsManager, IMemoryCache cacheService)
        {
            _logger = logger;
            _pluginsManager = pluginsManager;
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
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            //var added = await _pluginsManager.AddPluginFromFile(file);
            string added = "Not implemented yet";

            // current timestamp
            var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            // Save the file to disk: .zip
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", timestamp + ".zip");
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            added = await _pluginsManager.AddPluginFromFile(filePath);

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
