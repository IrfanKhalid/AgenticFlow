using AgentsAPI.BusinessLogic.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgentsAPI.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpPost("searchWithGP")]
        public async Task<IActionResult> SearchWithGP([FromBody] SearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest("Query is required.");
            }

            var results = await _searchService.SearchWithGoogleAsync(request.Query);
            return Ok(new { Results = results });
        }
    }

    public class SearchRequest
    {
        public required string Query { get; set; }
    }
}