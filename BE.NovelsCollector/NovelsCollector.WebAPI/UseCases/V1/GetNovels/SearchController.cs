﻿using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Application.UseCases.GetNovels;
using NovelsCollector.Application.UseCases.ManagePlugins;
using NovelsCollector.Domain.Entities.Plugins.Sources;
using NovelsCollector.Infrastructure.Persistence.Entities;

namespace NovelsCollector.WebAPI.UseCases.V1.GetNovels
{
    [ApiController]
    [Tags("02. Search")]
    [Route("api/v1/search")]
    public class SearchController : ControllerBase
    {
        #region Injected Services
        private readonly ILogger<SearchController> _logger;
        private readonly IEnumerable<SourcePlugin> _sourcesPlugins;

        public SearchController(ILogger<SearchController> logger, BasePluginsManager<SourcePlugin, ISourceFeature> sourcePluginManager)
        {
            _logger = logger;
            _sourcesPlugins = sourcePluginManager.Installed;
        }
        #endregion

        /// <summary>
        /// Search novels by source, keyword, title, author and page queries
        /// </summary>
        /// <param name="source"> The source of the novel (e.g., DTruyenCom, SSTruyenVn). </param>
        /// <param name="keyword"> The keyword to search for. </param>
        /// <param name="title" > The title keyword of the novel. </param>
        /// <param name="author"> The author keyword of the novel. </param>
        /// <param name="page"> The page number to search for. </param>
        /// <returns> The list of novels found by the search query. </returns>
        [HttpGet("{source}")]
        [EndpointSummary("Search novels by source, keyword, title, author and page queries")]
        public async Task<IActionResult> Get(
            [FromRoute] string source,
            [FromQuery] string? keyword, [FromQuery] string? title, [FromQuery] string? author,
            [FromQuery] int page = 1)
        {
            var (novels, totalPage) = await new SearchNovelsUC(_sourcesPlugins).Execute(source, keyword, title, author, page);

            return Ok(new
            {
                data = novels,
                meta = new
                {
                    source,
                    page,
                    totalPage,
                }
            });
        }



    }
}
