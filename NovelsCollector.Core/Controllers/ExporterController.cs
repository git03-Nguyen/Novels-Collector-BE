using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Services.Plugins;

namespace NovelsCollector.Core.Controllers
{
    [ApiController]
    [Tags("08. Exporters")]
    [Route("api/v1/exporter")]
    public class ExporterController : ControllerBase
    {
        #region Injected Services
        private readonly ILogger<ExporterController> _logger;
        private readonly ExporterPluginsManager _exporterPluginManager;

        public ExporterController(ILogger<ExporterController> logger, ExporterPluginsManager exporterPluginManager)
        {
            _logger = logger;
            _exporterPluginManager = exporterPluginManager;
        }
        #endregion

        // GET: api/v1/exporters
        [HttpGet]
        [EndpointSummary("Get a list of all exporter plugins")]
        public IActionResult GetExporters()
        {
            return Ok(new
            {
                data = _exporterPluginManager.Plugins.Values.ToArray(),
            });
        }

        // GET: api/v1/exporters/reload

        [HttpGet("reload")]
        [EndpointSummary("Reload exporter plugins")]
        public IActionResult Reload()
        {
            try
            {
                _exporterPluginManager.ReloadPlugins();
                return Ok(new
                {
                    data = _exporterPluginManager.Plugins.Values.ToArray(),
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = ex.HResult, message = ex.Message } });
            }
        }


    }
}
