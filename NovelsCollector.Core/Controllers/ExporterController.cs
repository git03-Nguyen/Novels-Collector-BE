using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Services.Plugins;
using NovelsCollector.SDK.Models;

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

        [HttpGet]
        [EndpointSummary("Get a list of all exporter plugins")]
        public IActionResult GetExporters()
        {
            return Ok(new
            {
                data = _exporterPluginManager.Plugins.Values.ToArray(),
                // TODO: add the unloaded/disabled plugins
            });
        }

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

        [HttpGet("test1")]
        [EndpointSummary("Test an exporter plugin (epub)")]
        public async Task<IActionResult> TestExporter([FromServices] SourcePluginsManager sourcePluginsManager)
        {
            var startChapter = 1;
            var lastChapter = 2;

            // Get the novel
            Novel? novel = await sourcePluginsManager.GetNovelDetail("TruyenFullVn", "tao-tac");

            // initiate novel.Chapters as an empty list
            var list = new List<Chapter>();

            // Get the chapters' content
            for (int i = startChapter; i <= lastChapter; i++)
            {
                var chapter = await sourcePluginsManager.GetChapter("TruyenFullVn", "tao-tac", $"chuong-{i}");
                if (chapter != null)
                {
                    list.Add(chapter);
                }
            }

            // Assign the list of chapters to novel.Chapters, now we have a complete novel
            novel.Chapters = list.ToArray();
            novel.Source = "TruyenFullVn";

            string? format = null;

            // Export the novel
            using (var stream = new FileStream("D:/SimpleEPub.epub", FileMode.Create))
            {
                format = await _exporterPluginManager.Export("SimpleEPub", novel, stream);
            }

            return Ok(new
            {
                data = new
                {
                    format,
                    path = "D:/SimpleEPub.epub",
                }
            });

        }

        [HttpGet("test2")]
        [EndpointSummary("Test an exporter plugin (pdf)")]
        public async Task<IActionResult> Test2Exporter([FromServices] SourcePluginsManager sourcePluginsManager)
        {
            var startChapter = 1;
            var lastChapter = 2;

            // Get the novel
            Novel? novel = await sourcePluginsManager.GetNovelDetail("TruyenFullVn", "tao-tac");

            // initiate novel.Chapters as an empty list
            var list = new List<Chapter>();

            // Get the chapters' content
            for (int i = startChapter; i <= lastChapter; i++)
            {
                var chapter = await sourcePluginsManager.GetChapter("TruyenFullVn", "tao-tac", $"chuong-{i}");
                if (chapter != null)
                {
                    list.Add(chapter);
                }
            }

            // Assign the list of chapters to novel.Chapters, now we have a complete novel
            novel.Chapters = list.ToArray();
            novel.Source = "TruyenFullVn";

            string? format = null;

            // Export the novel
            using (var stream = new FileStream("D:/SimplePDF.pdf", FileMode.Create))
            {
                format = await _exporterPluginManager.Export("SimplePDF", novel, stream);
            }

            return Ok(new
            {
                data = new
                {
                    format,
                    path = "D:/SimplePDF.pdf",
                }
            });

        }

        [HttpGet("test3")]
        [EndpointSummary("Test an exporter plugin (mobi)")]
        public async Task<IActionResult> Test3Exporter([FromServices] SourcePluginsManager sourcePluginsManager)
        {
            var startChapter = 1;
            var lastChapter = 2;

            // Get the novel
            Novel? novel = await sourcePluginsManager.GetNovelDetail("TruyenFullVn", "tao-tac");

            // initiate novel.Chapters as an empty list
            var list = new List<Chapter>();

            // Get the chapters' content
            for (int i = startChapter; i <= lastChapter; i++)
            {
                var chapter = await sourcePluginsManager.GetChapter("TruyenFullVn", "tao-tac", $"chuong-{i}");
                if (chapter != null)
                {
                    list.Add(chapter);
                }
            }

            // Assign the list of chapters to novel.Chapters, now we have a complete novel
            novel.Chapters = list.ToArray();
            novel.Source = "TruyenFullVn";

            string? format = null;

            // Export the novel
            using (var stream = new FileStream("D:/SimpleMobi.mobi", FileMode.Create))
            {
                format = await _exporterPluginManager.Export("SimpleMobi", novel, stream);
            }

            return Ok(new
            {
                data = new
                {
                    format,
                    path = "D:/SimpleMobi.mobi",
                }
            });

        }


    }
}
