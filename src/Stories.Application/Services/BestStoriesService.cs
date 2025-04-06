using Microsoft.Extensions.Logging;
using Stories.Application.Clients;
using Stories.Application.Dto;
using Stories.Application.Mappers;

namespace Stories.Application.Services;

public sealed class BestStoriesService : IBestStoriesService
{
    private const int SIMULTANEOUS_BEST_STORIES_REQUESTS = 30;
    private const int SIMULTANEOUS_STORY_REQUESTS = 20;

    private static readonly SemaphoreSlim BestStoriesSemaphore = new SemaphoreSlim(SIMULTANEOUS_BEST_STORIES_REQUESTS, SIMULTANEOUS_BEST_STORIES_REQUESTS);

    private readonly ILogger<BestStoriesService> _logger;
    private readonly IHackerNewsClient _hackerNewsClient;

    public BestStoriesService(ILogger<BestStoriesService> logger,  IHackerNewsClient hackerNewsClient)
    {
        _logger = logger;
        _hackerNewsClient = hackerNewsClient;
    }

    public async Task<BestStoriesResult> GetBestStoriesAsync(int limit, CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return BestStoriesResult.InvalidArgument;
        }

        HackerNewsStoryDto[] stories;
        try
        {
            var bestStoriesIds = await GetBestStoriesIds(cancellationToken);

            stories = await GetStories(bestStoriesIds, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An exception occured getting stories, limit: {Limit} ", limit);
            return BestStoriesResult.HackerNewsServiceError;
        }
        
        // we assume that best stories are not sorted,
        // that's why we load all stories, sort them by Score and only then take a required number of items 
        var bestStories = stories
            .OrderByDescending(s => s.Score)
            .Take(limit)
            .Select(StoryMapper.Map)
            .ToList();

        return new BestStoriesResult(bestStories);
    }

    private async Task<int[]> GetBestStoriesIds(CancellationToken cancellationToken)
    {
        try
        {
            await BestStoriesSemaphore.WaitAsync(cancellationToken);
            return await _hackerNewsClient.GetBestStoriesIds(cancellationToken);
        }
        finally
        {
            BestStoriesSemaphore.Release();
        }
    }
    
    private async Task<HackerNewsStoryDto[]> GetStories(int[] storiesIds, CancellationToken cancellationToken)
    {
        using (var semaphore = new SemaphoreSlim(SIMULTANEOUS_STORY_REQUESTS, SIMULTANEOUS_STORY_REQUESTS))
        {
            var tasks = storiesIds.Select(async id =>
            {
                try
                {
                    await semaphore.WaitAsync(cancellationToken);
                    return await _hackerNewsClient.GetStory(id, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            });
            return await Task.WhenAll(tasks);
        } 
    }
    
}