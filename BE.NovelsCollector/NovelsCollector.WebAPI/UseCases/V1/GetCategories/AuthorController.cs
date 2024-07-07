using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Application.UseCases.GetCategories;
using NovelsCollector.Application.UseCases.ManagePlugins;
using NovelsCollector.Domain.Entities.Plugins.Sources;
using NovelsCollector.Infrastructure.Persistence.Entities;

namespace NovelsCollector.WebAPI.UseCases.V1.GetCategories
{
    [ApiController]
    [Tags("04. Author")]
    [Route("api/v1/author")]
    public class AuthorController : ControllerBase
    {
        #region Injected Services
        private readonly ILogger<AuthorController> _logger;
        private readonly IEnumerable<SourcePlugin> _sourcesPlugins;

        public AuthorController(ILogger<AuthorController> logger, BasePluginsManager<SourcePlugin, ISourceFeature> sourcePluginManager)
        {
            _logger = logger;
            _sourcesPlugins = sourcePluginManager.Installed;
        }
        #endregion

        /// <summary>
        /// Get novels by author from a source
        /// </summary>
        /// <param name="source"> The source name. e.g. 'TruyenFullVn' </param>
        /// <param name="authorSlug"> The author slug. e.g. 'tieu-tinh' </param>
        /// <param name="page"> The page number. Default is 1 </param>
        /// <returns> A list of novels by author from a source </returns>
        [HttpGet("{source}/{authorSlug}")]
        [EndpointSummary("Get novels by author from a source")]
        public async Task<IActionResult> Get([FromRoute] string source, [FromRoute] string authorSlug, [FromQuery] int page = 1)
        {
            var (novels, totalPage) = await new GetAuthorNovelsUC(_sourcesPlugins).Execute(source, authorSlug, page);

            return Ok(new
            {
                data = novels,
                meta = new
                {
                    source,
                    authorSlug,
                    page,
                    totalPage
                }
            });
        }
    }
}
