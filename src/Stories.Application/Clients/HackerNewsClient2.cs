using System.Text.Json;
using Stories.Application.Dto;

namespace Stories.Application.Clients;

public sealed class HackerNewsClient2: IHackerNewsClient
{
    private readonly HttpClient _httpClient;

    public HackerNewsClient2(HttpClient httpClient)
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

    private async Task<T> GetFromJson<T>(string uri, CancellationToken cancellationToken) where T : class
    {
        var response = await _httpClient.GetAsync(uri, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync<T>(stream, JsonSerializerOptions.Default, cancellationToken);
            if(result == null)
            {
                throw new EmptyResponseException($"Got the empty response from uri: {uri}");
            }
            return result;
        }

        throw new InvalidStatusException($"The uri: {uri} returned an invalid status: {response.StatusCode}");
    }
}