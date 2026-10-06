using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Santander_Developer_Test_API.Services;

namespace Santander_Developer_Test_API.Controllers
{
    [Route("api/BestStories")]
    [ApiController]
    public class BestStoriesController : ControllerBase
    {
        private readonly IHackerService _service;
        public BestStoriesController(IHackerService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType(typeof(Models.StoryResponse[]), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Get([FromQuery] int n)
        {
            if (n <= 0)
            {
                return BadRequest(new { error = "n must be greater than 0" });
            }

            var result = await _service.GetBestStoriesAsync(n);
            return Ok(result);
        }
    }
}
