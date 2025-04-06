using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Stories.Application.Clients;
using Stories.Application.Services;
using Xunit;

namespace Stories.Application.UnitTest.IntegrationTests;

// TODO: move to integration tests
public class TestHackerNewsClient
{
    private readonly ITestOutputHelper _output;

    public TestHackerNewsClient(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task HackerNewsClient()
    {
        var httpClient = new HttpClient()
        {
            BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/")
        };
        var client = new HackerNewsClient(httpClient);
        var r = await client.GetBestStoriesIds(CancellationToken.None);
        Assert.NotNull(r);
        
        _output.WriteLine(JsonSerializer.Serialize(r));
    }
    
}

public class TestBestStoriesService
{
    private readonly ITestOutputHelper _output;

    public TestBestStoriesService(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task BestStoriesService()
    {
        var logger = new Mock<ILogger<BestStoriesService>>();
        var httpClient = new HttpClient()
        {
            BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/")
        };
        var client = new HackerNewsClient(httpClient);
        
        var service = new BestStoriesService(logger.Object, client);
        var r = await service.GetBestStoriesAsync(5, CancellationToken.None);
        
        Assert.NotNull(r);
        
        _output.WriteLine(JsonSerializer.Serialize(r, new JsonSerializerOptions() { WriteIndented = true }));
    }
}