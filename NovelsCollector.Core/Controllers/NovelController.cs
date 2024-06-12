using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Exceptions;
using NovelsCollector.Core.Models;
using NovelsCollector.Core.Services;
using NovelsCollector.SDK.Models;

namespace NovelsCollector.Core.Controllers
{
    [ApiController]
    [Tags("05. Novel")]
    [Route("api/v1/novel")]
    public class NovelController : ControllerBase
    {

        #region Injected Services

        private readonly ILogger<NovelController> _logger;
        private readonly SourcePluginsManager _sourcesPlugins;
        private readonly ExporterPluginsManager _exporterPlugins;

        public NovelController(ILogger<NovelController> logger, SourcePluginsManager sourcePluginManager, ExporterPluginsManager exporterPluginManager)
        {
            _logger = logger;
            _sourcesPlugins = sourcePluginManager;
            _exporterPlugins = exporterPluginManager;
        }

        #endregion

        /// <summary>
        /// View brief information of a novel
        /// </summary>
        /// <param name="source">The source of the novel (e.g., DTruyenCom, SSTruyenVn).</param>
        /// <param name="novelSlug">The slug identifier for the novel (e.g., tao-tac).</param>
        /// <returns> Brief information of the novel. </returns>
        [HttpGet("{source}/{novelSlug}")]
        [EndpointSummary("View brief information of a novel")]
        public async Task<IActionResult> GetNovel([FromRoute] string source, [FromRoute] string novelSlug)
        {
            var novel = await _sourcesPlugins.GetNovelDetail(source, novelSlug);

            // Check if the novel is not found
            if (novel == null) throw new NotFoundException("Không tìm thấy truyện này.");

            // Find novel in other sources
            var otherSources = await _sourcesPlugins.GetNovelFromOtherSources(source, novel);

            // Return the novel
            return Ok(new
            {
                data = novel,
                meta = new
                {
                    source,
                    otherSources,
                }
            });
        }

        /// <summary>
        /// View a list of chapters of a novel by page
        /// </summary>
        /// <param name="source">The source of the novel (e.g., DTruyenCom, SSTruyenVn).</param>
        /// <param name="novelSlug">The slug identifier for the novel (e.g., tao-tac).</param>
        /// <param name="page">The page number of the chapters list (default is the last page = -1).</param>
        /// <returns> A list of chapters of a novel by page. </returns>
        [HttpGet("{source}/{novelSlug}/chapters")]
        [EndpointSummary("View chapters of a novel by page, -1 is last page")]
        public async Task<IActionResult> GetChapters([FromRoute] string source, [FromRoute] string novelSlug, [FromQuery] int page = -1)
        {
            // Check if the page is invalid
            if (page == 0) throw new BadHttpRequestException("Trang không hợp lệ.");

            // Get the chapters
            var (chapters, totalPage) = await _sourcesPlugins.GetChaptersList(source, novelSlug, page);

            // Check if the novel is not found
            if (chapters == null) throw new NotFoundException("Không tìm thấy chương cho truyện này.");

            // Return the chapters
            return Ok(new
            {
                data = chapters,
                meta = new
                {
                    source,
                    novelSlug,
                    page = (page != -1) ? page : totalPage,
                    totalPage,
                }
            });
        }

        /// <summary>
        /// Export some chapters of a novel to a file (e.g., epub, pdf).
        /// </summary>
        /// <param name="source"> The source of the novel (e.g., DTruyenCom, SSTruyenVn). </param>
        /// <param name="novelSlug"> The slug identifier for the novel (e.g., tao-tac). </param>
        /// <param name="request"> The request object contains the plugin name and the list of chapters to export. </param>
        /// <returns> The path to the exported file. </returns>
        /// <exception cref="BadHttpRequestException"> If the list of chapters is invalid. </exception>
        /// <exception cref="NotFoundException"> If the plugin is not found. </exception>
        [HttpPost("{source}/{novelSlug}/export")]
        [EndpointSummary("Export some chapters of a novel to a file")]
        public async Task<IActionResult> ExportChapters([FromRoute] string source, [FromRoute] string novelSlug, [FromBody] ExportRequest request)
        {
            // Check if no chapters are provided
            if (request.ChapterSlugs.Count == 0)
                throw new BadHttpRequestException("Danh sách chương không hợp lệ.");

            // Check if no plugin is found
            if (!_exporterPlugins.Installed.Any(x => x.Name == request.Plugin.Name))
                throw new NotFoundException($"Không tìm thấy plugin {request.Plugin.Name}.");

            // DEBUG mode: number of chapters to export is <= 10
            const int maxChapters = 10;
            if (request.ChapterSlugs.Count > maxChapters)
                throw new BadHttpRequestException($"Số lượng chương xuất không được vượt quá {maxChapters} chương.");

            // Get the novel
            Novel? novel = await _sourcesPlugins.GetNovelDetail(source, novelSlug);
            if (novel == null)
                throw new NotFoundException("Không tìm thấy truyện này.");

            // Get the chapters' content
            var listChapters = new List<Chapter>();
            foreach (var slug in request.ChapterSlugs)
            {
                var chapter = await _sourcesPlugins.GetChapterContent(source, novelSlug, slug);
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
            using (var stream = new FileStream($"D:/{timestamp}.{request.Plugin.Extension}", FileMode.Create))
            {
                await _exporterPlugins.Export(request.Plugin.Name, novel, stream);
            }

            return Ok(new
            {
                data = new
                {
                    extension = request.Plugin.Extension,
                    path = $"D:/{timestamp}.{request.Plugin.Extension}",
                }
            });

        }

    }

    public class ExportRequest
    {
        public ExporterPlugin Plugin { get; set; }
        public List<string> ChapterSlugs { get; set; }
    }
}
