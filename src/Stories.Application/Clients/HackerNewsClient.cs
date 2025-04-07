using System.Net.Http.Json;
using Stories.Application.Dto;

namespace Stories.Application.Clients;

public interface IHackerNewsClient {
    Task<int[]> GetBestStoriesIds(CancellationToken cancellationToken);
    Task<HackerNewsStoryDto> GetStory(int id, CancellationToken cancellationToken);
}

public sealed class HackerNewsClient : IHackerNewsClient
{
    private readonly HttpClient _httpClient;

    public HackerNewsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<int[]> GetBestStoriesIds(CancellationToken cancellationToken)
    {
        return await GetFromJson<int[]>("beststories.json", cancellationToken);
    }
    
    public async Task<HackerNewsStoryDto> GetStory(int id, CancellationToken cancellationToken)
    {
        return await GetFromJson<HackerNewsStoryDto>($"item/{id}.json", cancellationToken);
    }

    private async Task<T> GetFromJson<T>(string uri, CancellationToken cancellationToken ) where T : class
    {
        var obj = await _httpClient.GetFromJsonAsync<T>(uri, cancellationToken);
        
        if (obj == null)
        {
            throw new EmptyResponseException($"Got the empty response from uri: {uri}");
        }
        return obj;
    }
}