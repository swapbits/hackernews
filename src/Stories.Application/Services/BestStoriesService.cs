using System.Diagnostics.CodeAnalysis;
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

public sealed class BestStoriesCachingService : IBestStoriesService
{
    private const int SIMULTANEOUS_BEST_STORIES_REQUESTS = 3;
    private const int SIMULTANEOUS_STORY_REQUESTS = 3;
    
    private static readonly SemaphoreSlim BestStoriesSemaphore = new SemaphoreSlim(SIMULTANEOUS_BEST_STORIES_REQUESTS, SIMULTANEOUS_BEST_STORIES_REQUESTS);
    
    private readonly IHackerNewsClient _hackerNewsClient;
    private readonly ICache _cache;

    private const int BEST_STORIES_ID_KEY = -1;
    
    public BestStoriesCachingService(IHackerNewsClient hackerNewsClient, ICache cache)
    {
        _hackerNewsClient = hackerNewsClient;
        _cache = cache;
    }

    public async Task<BestStoriesResult> GetBestStoriesAsync(int limit, CancellationToken cancellationToken)
    {
        var bestStoriesIds = await GetBestStoriesIds(cancellationToken);

        var limitedStoresIds = bestStoriesIds.Take(limit).ToArray(); 
        var stories = await GetStories(limitedStoresIds, cancellationToken);
        var bestStories = stories.Select(StoryMapper.Map).ToList();
        return new BestStoriesResult(bestStories);
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

public sealed class BestStoriesResult
{
    public required BestStoriesStatus Status { get; init; }

    public required IList<Story> Stories { get; init; }

    [SetsRequiredMembers]
    public BestStoriesResult(IList<Story> stories) : this(BestStoriesStatus.Ok, stories)
    {
    }

    [SetsRequiredMembers]
    private BestStoriesResult(BestStoriesStatus status, IList<Story> stories)
    {
        Status = status;
        Stories = stories;
    }

    public static readonly BestStoriesResult HackerNewsServiceError = new BestStoriesResult(BestStoriesStatus.HackerNewsServiceError, []);
    public static readonly BestStoriesResult InvalidArgument = new BestStoriesResult(BestStoriesStatus.InvalidArgument, []);
}

public enum BestStoriesStatus
{
    Ok = 1,
    InvalidArgument,
    HackerNewsServiceError
}