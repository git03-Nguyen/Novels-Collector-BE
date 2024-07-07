using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NovelsCollector.Application.Exceptions;
using NovelsCollector.Application.UseCases.GetNovels;
using NovelsCollector.Application.UseCases.ManagePlugins;
using NovelsCollector.Domain.Entities.Plugins.Sources;
using NovelsCollector.Domain.Resources.Novels;
using NovelsCollector.Infrastructure.Persistence.Entities;

namespace NovelsCollector.WebAPI.UseCases.V1.GetNovels
{
    [ApiController]
    [Tags("05. Novel")]
    [Route("api/v1/novel")]
    public class NovelController : ControllerBase
    {

        #region Injected Services

        private readonly ILogger<NovelController> _logger;
        private readonly IMemoryCache _cacheService;
        private readonly IEnumerable<SourcePlugin> _sourcesPlugins;

        public NovelController(ILogger<NovelController> logger, BasePluginsManager<SourcePlugin, ISourceFeature> sourcePluginManager, IMemoryCache cacheService)
        {
            _logger = logger;
            _cacheService = cacheService;
            _sourcesPlugins = sourcePluginManager.Installed;
        }

        #endregion

        /// <summary>
        /// View brief information of a novel
        /// </summary>
        /// <param name="source">The source of the novel (e.g., DTruyenCom, SSTruyenVn).</param>
        /// <param name="novelSlug">The slug identifier for the novel (e.g., tao-tac).</param>
        /// <returns> Brief information of the novel. </returns>
        /// <exception cref="NotFoundException"> If the novel is not found. </exception>
        [HttpGet("{source}/{novelSlug}")]
        [EndpointSummary("View brief information of a novel")]
        public async Task<IActionResult> GetNovel([FromRoute] string source, [FromRoute] string novelSlug)
        {
            var novel = await new GetDetailsUC(_sourcesPlugins).Execute(source, novelSlug);

            // Check if the novel is not found
            if (novel == null) throw new NotFoundException("Không tìm thấy truyện này.");

            // Return the novel
            return Ok(new
            {
                data = novel,
                meta = new
                {
                    source
                }
            });
        }

        /// <summary>
        /// Find the same novel in other sources
        /// </summary>
        /// <param name="source"> The source of the novel (e.g., DTruyenCom, SSTruyenVn). </param>
        /// <param name="novelSlug"> The slug identifier for the novel (e.g., tao-tac). </param>
        /// <param name="novel"> The novel object to find in other sources. </param>
        /// <returns> The same novel addresses in other sources. </returns>
        /// <exception cref="BadHttpRequestException"> If the novel is not provided. </exception>
        [HttpPost("{source}/{novelSlug}/others")]
        [EndpointSummary("Find the same novel in other sources")]
        public async Task<IActionResult> GetNovelFromOtherSources([FromRoute] string source, [FromRoute] string novelSlug, [FromBody] Novel? novel)
        {
            // Check if the novel is not provided
            if (novel == null) throw new BadHttpRequestException("Truyện không hợp lệ.");
            novel.Slug = novelSlug;

            Dictionary<string, Novel>? novels = null;

            // reassign the novel.Title if: Tào Tặc - 曹贼 in TruyenTangThuVienVn => Tào Tặc
            if (source == "TruyenTangThuVienVn" && novel.Title.Contains(" - "))
            {
                novel.Title = novel.Title.Split(" - ")[0].Trim();
            }

            // Caching the same novels in all sources
            var cacheKey = $"novels-{novel.Title.Trim()}";
            if (_cacheService.TryGetValue(cacheKey, out novels))
            {
                _logger.LogInformation($"Cache hit for novels of {novel.Title}");
                // copy the cachedNovels to a new dictionary, without the excluded source
            }
            else
            {
                // Find novel in all sources
                novels = await new GetSameNovelsUC(_sourcesPlugins).Execute(source, novel);

                // Cache the same novels in all sources
                _logger.LogInformation($"Cache miss for novels of {novel.Title}. Caching...");
                _cacheService.Set(cacheKey, novels, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                    SlidingExpiration = TimeSpan.FromMinutes(30),
                    Size = 1
                });

            }

            // Return the novels
            var result = novels?.Where(kvp => kvp.Key != source).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Return the novel
            return Ok(new
            {
                data = result,
                meta = new
                {
                    source,
                    novelSlug,
                    title = novel.Title,
                    author = novel.Authors?[0].Name,
                }
            });
        }

        /// <summary>
        /// View a list of chapters of a novel
        /// </summary>
        /// <param name="source">The source of the novel (e.g., DTruyenCom, SSTruyenVn).</param>
        /// <param name="novelSlug">The slug identifier for the novel (e.g., tao-tac).</param>
        /// <param name="novelId">The identifier for the novel.</param>
        /// <returns> A list of chapters of a novel by page. </returns>
        /// <exception cref="BadHttpRequestException"> If the page is invalid. </exception>
        [HttpGet("{source}/{novelSlug}/{novelId}/chapters")]
        [EndpointSummary("View chapters of a novel")]
        public async Task<IActionResult> GetChapters([FromRoute] string source, [FromRoute] string novelSlug, [FromRoute] string novelId)
        {

            // Get the chapters
            var chapters = await new GetChaptersListUC(_sourcesPlugins).Execute(source, novelSlug, novelId);

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
                    id = novelId
                }
            });
        }



    }

}
