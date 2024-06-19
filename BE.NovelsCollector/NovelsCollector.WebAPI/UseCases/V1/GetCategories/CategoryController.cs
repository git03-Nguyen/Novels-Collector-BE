using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NovelsCollector.Application.UseCases.GetCategories;
using NovelsCollector.Application.UseCases.ManagePlugins;
using NovelsCollector.Domain.Entities.Plugins.Sources;
using NovelsCollector.Domain.Resources.Categories;
using NovelsCollector.Domain.Resources.Novels;
using NovelsCollector.Infrastructure.Persistence.Entities;

namespace NovelsCollector.WebAPI.UseCases.V1.GetCategories
{
    [ApiController]
    [Tags("03. Category")]
    [Route("api/v1/category")]
    public class CategoryController : ControllerBase
    {
        #region Injected Services
        private readonly ILogger<CategoryController> _logger;
        private readonly IMemoryCache _cacheService;
        private readonly IEnumerable<SourcePlugin> _sourcesPlugins;

        public CategoryController(ILogger<CategoryController> logger, BasePluginsManager<SourcePlugin, ISourceFeature> sourcePluginManager, IMemoryCache cacheService)
        {
            _logger = logger;
            _cacheService = cacheService;
            _sourcesPlugins = sourcePluginManager.Installed;
        }
        #endregion

        /// <summary>
        /// Get all categories from a source
        /// </summary>
        /// <param name="source"> The source name. e.g. 'TruyenFullVn' </param>
        /// <returns> A list of categories from a source </returns>
        [HttpGet("{source}")]
        [EndpointSummary("Get all categories from a source")]
        public async Task<IActionResult> Get([FromRoute] string source)
        {
            // Caching the categories
            var cacheKey = $"categories-{source}";
            if (_cacheService.TryGetValue(cacheKey, out Category[]? categories))
            {
                _logger.LogInformation($"Cache hit for categories of {source}");
            }
            else
            {
                categories = await new GetCategoriesListUC(_sourcesPlugins).Execute(source) ?? Array.Empty<Category>();
                // Cache the categories
                if (categories.Length > 0)
                {
                    _logger.LogInformation($"Cache miss for categories of {source}. Caching...");
                    _cacheService.Set(cacheKey, categories, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                        SlidingExpiration = TimeSpan.FromMinutes(15),
                        Size = 1
                    });
                }
            }

            return Ok(new
            {
                data = categories,
                meta = new { source }
            });
        }

        /// <summary>
        /// Get novels by category from a source
        /// </summary>
        /// <param name="source"> The source name. e.g. 'TruyenFullVn' </param>
        /// <param name="categorySlug"> The category slug. e.g. 'ngon-tinh' or 3 special categories: 'hot', 'latest', 'completed' </param>
        /// <param name="page"> The page number. Default is 1 </param>
        /// <returns></returns>
        [HttpGet("{source}/{categorySlug}")]
        [EndpointSummary("Get novels by category from a source. *Special categories: 'hot', 'latest', 'completed'")]
        public async Task<IActionResult> Get([FromRoute] string source, [FromRoute] string categorySlug, [FromQuery] int page = 1)
        {
            Novel[]? novels = null;
            int totalPage = -1;

            bool flag = false;

            // Caching the novels for page 1
            if (page == 1)
            {
                var cacheKey = $"novels-{source}-{categorySlug}-page1";
                var cacheKeyTotalPage = $"novels-{source}-{categorySlug}-totalPage";

                if (_cacheService.TryGetValue(cacheKey, out novels) && _cacheService.TryGetValue(cacheKeyTotalPage, out totalPage))
                {
                    _logger.LogInformation($"Cache hit for novels of {source} in category {categorySlug} at page 1");
                    flag = true;
                }
            }

            if (!flag)
            {
                switch (categorySlug)
                {
                    case "hot":
                        (novels, totalPage) = await new GetHotNovelsUC(_sourcesPlugins).Execute(source, page);
                        break;

                    case "latest":
                        (novels, totalPage) = await new GetLatestNovelsUC(_sourcesPlugins).Execute(source, page);
                        break;

                    case "completed":
                        (novels, totalPage) = await new GetCompletedNovelsUC(_sourcesPlugins).Execute(source, page);
                        break;

                    default:
                        (novels, totalPage) = await new GetCategoryNovelsUC(_sourcesPlugins).Execute(source, categorySlug, page);
                        break;
                }

                // Cache the novels for page 1
                if (novels != null && novels.Length > 0)
                {
                    _logger.LogInformation($"Cache miss for novels of {source} in category {categorySlug} at page 1. Caching...");
                    var cacheKey = $"novels-{source}-{categorySlug}-page1";
                    var cacheKeyTotalPage = $"novels-{source}-{categorySlug}-totalPage";

                    _cacheService.Set(cacheKey, novels, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                        SlidingExpiration = TimeSpan.FromMinutes(15),
                        Size = 1
                    });
                    _cacheService.Set(cacheKeyTotalPage, totalPage, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                        SlidingExpiration = TimeSpan.FromMinutes(15),
                        Size = 1
                    });

                }

            }

            return Ok(new
            {
                data = novels,
                meta = new
                {
                    source,
                    categorySlug,
                    page,
                    totalPage
                }
            });
        }

    }
}
