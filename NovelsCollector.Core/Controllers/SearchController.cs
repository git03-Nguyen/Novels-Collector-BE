﻿using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Services.Plugins;

namespace NovelsCollector.Core.Controllers
{
    [ApiController]
    [Tags("02. Search")]
    [Route("api/v1/search")]
    public class SearchController : ControllerBase
    {
        #region Injected Services
        private readonly ILogger<SearchController> _logger;
        private readonly SourcePluginsManager _sourcePluginManager;

        public SearchController(ILogger<SearchController> logger, SourcePluginsManager sourcePluginManager)
        {
            _logger = logger;
            _sourcePluginManager = sourcePluginManager;
        }
        #endregion

        [HttpGet]
        [EndpointSummary("Search novels by source, keyword, author, year and page queries")]
        public async Task<IActionResult> Get(
            [FromQuery] string source,
            [FromQuery] string keyword, [FromQuery] string? author, [FromQuery] string? year,
            [FromQuery] int page = 1)
        {
            try
            {
                var (novels, totalPage) = await _sourcePluginManager.Search(source, keyword, author, year, page);
                return Ok(new
                {
                    data = novels,
                    meta = new
                    {
                        page = page,
                        totalPage = totalPage,
                        source = source,
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = ex.HResult, message = ex.Message } });
            }
        }



    }
}
