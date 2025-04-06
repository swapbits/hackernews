using Microsoft.AspNetCore.Mvc;
using Stories.Application.Dto;
using Stories.Application.Services;

namespace Stories.Api.Controllers;

[Route("[controller]")]
[ApiController]
public class StoryController : ControllerBase
{
    private readonly IBestStoriesService _storiesService;

    public StoryController(IBestStoriesService storiesService)
    {
        _storiesService = storiesService;
    }

    [HttpGet("best/{limit}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IList<Story>>> GetBestStories(int limit, CancellationToken cancellationToken)
    {
        var result = await _storiesService.GetBestStoriesAsync(limit, cancellationToken);
        return result.Status switch
        {
            BestStoriesStatus.Ok => Ok(result.Stories),
            BestStoriesStatus.InvalidArgument => ValidationProblem(),
            BestStoriesStatus.HackerNewsServiceError => StatusCode(StatusCodes.Status502BadGateway),
            _ => throw new NotImplementedException($"Unknown status code: {result.Status}")
        };
    }
}
