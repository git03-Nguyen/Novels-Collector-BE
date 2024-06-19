using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Application.Exceptions;
using NovelsCollector.Application.UseCases.ExportNovel;
using NovelsCollector.Application.UseCases.GetChapter;
using NovelsCollector.Application.UseCases.GetNovels;
using NovelsCollector.Application.UseCases.ManagePlugins;
using NovelsCollector.Domain.Entities.Plugins.Exporters;
using NovelsCollector.Domain.Entities.Plugins.Sources;
using NovelsCollector.Domain.Resources.Chapters;
using NovelsCollector.Domain.Resources.Novels;
using NovelsCollector.Infrastructure.Persistence.Entities;

namespace NovelsCollector.WebAPI.UseCases.V1.ExportNovel
{
    [ApiController]
    [Tags("05. Novel")]
    [Route("api/v1/novel")]
    public class ExportController : ControllerBase
    {
        #region Injected Services

        private readonly ILogger<ExportController> _logger;
        private readonly IEnumerable<ExporterPlugin> _exporterPlugins;
        private readonly IEnumerable<SourcePlugin> _sourcesPlugins;

        public ExportController(ILogger<ExportController> logger, BasePluginsManager<ExporterPlugin, IExporterFeature> exporterPluginManager, BasePluginsManager<SourcePlugin, ISourceFeature> sourcePluginManager)
        {
            _logger = logger;
            _exporterPlugins = exporterPluginManager.Installed;
            _sourcesPlugins = sourcePluginManager.Installed;
        }

        #endregion


        /// <summary>
        /// Export some chapters of a novel to a file (e.g., epub, pdf).
        /// </summary>
        /// <param name="source"> The source of the novel (e.g., DTruyenCom, SSTruyenVn). </param>
        /// <param name="novelSlug"> The slug identifier for the novel (e.g., tao-tac). </param>
        /// <param name="pluginName"> The name of the exporter plugin to use. </param>
        /// <param name="exportSlugs"> The list of chapter slugs to export. </param>
        /// <returns> The path to the exported file. </returns>
        /// <exception cref="BadHttpRequestException"> If the list of chapters is invalid. </exception>
        /// <exception cref="NotFoundException"> If the plugin is not found. </exception>
        [HttpPost("{source}/{novelSlug}/export/{pluginName}")]
        [EndpointSummary("Export a list of chapters of a novel to a file")]
        public async Task<IActionResult> ExportChapters([FromRoute] string source, [FromRoute] string novelSlug, [FromRoute] string pluginName,
            [FromBody] List<string> exportSlugs)
        {
            // DEBUG mode: number of chapters to export is <= 10
            const int maxChapters = 10;

            // Check if the list of chapters is invalid
            if (exportSlugs.Count == 0)
                throw new BadHttpRequestException("Danh sách chương yêu cầu không hợp lệ.");
            if (exportSlugs.Count > maxChapters)
                throw new BadHttpRequestException($"Số lượng chương xuất không được vượt quá {maxChapters} chương.");

            // Check if no plugin is found
            if (!_exporterPlugins.Any(x => x.Name == pluginName))
                throw new NotFoundException($"Không tìm thấy plugin {pluginName}.");

            // Get the novel
            //Novel? novel = await _sourcesPlugins.GetNovelDetail(source, novelSlug);
            Novel? novel = await new GetDetailsUC(_sourcesPlugins).Execute(source, novelSlug);
            if (novel == null)
                throw new NotFoundException("Không tìm thấy truyện này.");

            // Get the chapters' content
            var listChapters = new List<Chapter>();
            foreach (var slug in exportSlugs)
            {
                var chapter = await new GetChapterContentUC(_sourcesPlugins).Execute(source, novelSlug, slug);
                if (chapter != null)
                    listChapters.Add(chapter);
            }

            // Assign the list of chapters to novel.Chapters, now we have a complete novel
            novel.Chapters = listChapters.ToArray();
            novel.Source = source;

            // Get the timestamp for the random file name
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            // Export the novel
            // TODO: save the file to the cloud storage
            var plugin = _exporterPlugins.First(x => x.Name == pluginName);

            //using (var stream = new FileStream($"D:/{timestamp}.{plugin.Extension}", FileMode.Create))
            //{
            //    await _exporterPlugins.Export(pluginName, novel, stream);
            //}

            // Save the file to the wwwroot folder
            var day = DateTime.Now.ToString("yyyyMMdd");
            var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", day, plugin.Extension);
            // Create multiple folders if not exists
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var path = Path.Combine(folder, $"{timestamp}.{plugin.Extension}");
            using (var stream = new FileStream(path, FileMode.Create))
            {
                await new ExportNovelUC(_exporterPlugins).Execute(pluginName, novel, stream);
            }

            // get the host url
            var request = HttpContext.Request;
            var host = $"{request.Scheme}://{request.Host}";

            return Ok(new
            {
                data = new
                {
                    plugin = pluginName,
                    extension = plugin.Extension,
                    path = $"{host}/static/{day}/{plugin.Extension}/{timestamp}.{plugin.Extension}"
                }
            });

        }

    }


}
