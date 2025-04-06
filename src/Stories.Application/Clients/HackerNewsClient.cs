using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Stories.Application.Dto;

namespace Stories.Application.Clients;

public interface IHackerNewsClient {
    Task<int[]> GetBestStoriesIds(CancellationToken cancellationToken);
    Task<HackerNewsStoryDto> GetStory(int id, CancellationToken cancellationToken);
}

public class EmptyResponseException : Exception
{
    public EmptyResponseException(string msg) : base(msg) 
    {
    }
}

public sealed class HackerNewsClient : IHackerNewsClient
{
    // private readonly ILogger<HackerNewsClient> _logger;
    private readonly HttpClient _httpClient;

    public HackerNewsClient(HttpClient httpClient)
    {
        // _logger = logger;
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
        T? obj;
        try
        {
            obj = await _httpClient.GetFromJsonAsync<T>(uri, cancellationToken);
        }
        catch (Exception e)
        {
            throw;
        }
        
        if (obj == null)
        {
            throw new EmptyResponseException($"Got the empty response from uri: {uri}");
        }
        return obj;
    }

    private async Task<T> Get<T>(string url, CancellationToken cancellationToken) where T : class
    {
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync<T>(stream, JsonSerializerOptions.Default, cancellationToken);
            if(result == null)
            {
                throw new Exception();
            }
            return result;
        }

        throw new Exception();
    }
}
