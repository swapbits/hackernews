using Microsoft.Extensions.Logging;
using Stories.Application.Clients;
using Stories.Application.Dto;
using Stories.Application.Mappers;

namespace Stories.Application.Services;

public sealed class BestStoriesCachingService : IBestStoriesService
{
    private const int SIMULTANEOUS_BEST_STORIES_REQUESTS = 3;
    private const int SIMULTANEOUS_STORY_REQUESTS = 3;
    private const int BEST_STORIES_ID_KEY = -1;
    
    private static readonly SemaphoreSlim BestStoriesSemaphore = new SemaphoreSlim(SIMULTANEOUS_BEST_STORIES_REQUESTS, SIMULTANEOUS_BEST_STORIES_REQUESTS);

    private readonly ILogger<BestStoriesCachingService> _logger;
    private readonly IHackerNewsClient _hackerNewsClient;
    private readonly ICache _cache;
    
    public BestStoriesCachingService(
        ILogger<BestStoriesCachingService> logger,
        IHackerNewsClient hackerNewsClient,
        ICache cache)
    {
        _logger = logger;
        _hackerNewsClient = hackerNewsClient;
        _cache = cache;
    }

    public async Task<BestStoriesResult> GetBestStoriesAsync(int limit, CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return BestStoriesResult.InvalidArgument;
        }

        try
        {
            var bestStoriesIds = await GetBestStoriesIds(cancellationToken);

            var limitedStoresIds = bestStoriesIds.Take(limit).ToArray(); 
            var stories = await GetStories(limitedStoresIds, cancellationToken);
            var bestStories = stories
                .OrderByDescending(dto => dto.Score)
                .Select(StoryMapper.Map)
                .ToList();
            return new BestStoriesResult(bestStories);
        }
        catch(Exception e)
        {
            _logger.LogError(e, "An exception occured getting stories, limit: {Limit} ", limit);
            return BestStoriesResult.HackerNewsServiceError;
        }
    }
    
    private async Task<int[]> GetBestStoriesIds(CancellationToken cancellationToken)
    {
        int[]? bestStoryIds = _cache.Get<int[]?>(BEST_STORIES_ID_KEY);
        if (bestStoryIds == null)
        {
            
            try
            {
                await BestStoriesSemaphore.WaitAsync(cancellationToken);
                bestStoryIds = await _hackerNewsClient.GetBestStoriesIds(cancellationToken);
                _cache.Put(BEST_STORIES_ID_KEY, bestStoryIds);
            }
            finally
            {
                BestStoriesSemaphore.Release();
            }
        }
        return bestStoryIds;
    }

    private async Task<HackerNewsStoryDto[]> GetStories(int[] storiesIds, CancellationToken cancellationToken)
    {
        using (var semaphore = new SemaphoreSlim(SIMULTANEOUS_STORY_REQUESTS, SIMULTANEOUS_STORY_REQUESTS))
        {
            var tasks = storiesIds.Select(async id =>
            {
                var story = _cache.Get<HackerNewsStoryDto>(id);
                if (story != null)
                {
                    return story;
                }
                
                try
                {
                    await semaphore.WaitAsync(cancellationToken);
                    story = await _hackerNewsClient.GetStory(id, cancellationToken);
                    _cache.Put(id, story);
                    return story;
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