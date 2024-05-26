using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace NovelsCollector.Core.Controllers
{
    [Route("api/v1/search")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ILogger<SearchController> _logger;

        public SearchController(ILogger<SearchController> logger)
        {
            _logger = logger;
        }

        // GET: api/v1/search?keyword=keyword&author=author&year=year: search for novels by keyword, author, and year
        [HttpGet]
        public IActionResult Get([FromQuery] string? keyword, [FromQuery] string? author, [FromQuery] string? year)
        {
            if (keyword == null && author == null && year == null)
            {
                return BadRequest("At least one of the following query parameters must be provided: keyword, author, year");
            }
            return Ok($"Search for novels by keyword: {keyword}, author: {author}, and year: {year}");
        }

        
    }
}
