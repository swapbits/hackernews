using Microsoft.Extensions.Logging;
using Moq;
using Stories.Application.Clients;
using Stories.Application.Dto;
using Stories.Application.Services;
using Xunit;

namespace Stories.Application.UnitTest;

public class TestBestStoriesService
{
    [Fact]
    public async Task When_ThereAreNoBestStories_Expect_EmptyResult()
    {
        // arrange
        var logger = new Mock<ILogger<BestStoriesService>>();
        
        var hackerNewsClient = new Mock<IHackerNewsClient>();
        hackerNewsClient
            .Setup(client => client.GetBestStoriesIds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => []);
        
        var service = new BestStoriesService(logger.Object, hackerNewsClient.Object);
        
        // act
        var result = await service.GetBestStoriesAsync(10, CancellationToken.None);

        // assert
        Assert.Equal(BestStoriesStatus.Ok, result.Status);
        Assert.Empty(result.Stories);
        hackerNewsClient.Verify(client => client.GetBestStoriesIds(It.IsAny<CancellationToken>()), Times.Once);
        hackerNewsClient.Verify(client => client.GetStory(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task When_LimitIsNotPositive_Expect_InvalidArgument(int limit)
    {
        // arrange
        var logger = new Mock<ILogger<BestStoriesService>>();
        
        var hackerNewsClient = new Mock<IHackerNewsClient>();
        hackerNewsClient
            .Setup(client => client.GetBestStoriesIds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => []);
        
        var service = new BestStoriesService(logger.Object, hackerNewsClient.Object);
        
        // act
        var result = await service.GetBestStoriesAsync(limit, CancellationToken.None);

        // assert
        Assert.Equal(BestStoriesStatus.InvalidArgument, result.Status);
        Assert.Empty(result.Stories);
        hackerNewsClient.Verify(client => client.GetBestStoriesIds(It.IsAny<CancellationToken>()), Times.Never);
        hackerNewsClient.Verify(client => client.GetStory(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task When_HackerNewsClientGetIdsThrowsException_Expect_HackerNewsServiceError()
    {
        // arrange
        var logger = new Mock<ILogger<BestStoriesService>>();

        var hackerNewsClient = new Mock<IHackerNewsClient>();
        hackerNewsClient
            .Setup(client => client.GetBestStoriesIds(It.IsAny<CancellationToken>()))
            .Throws<TimeoutException>();
        
        var service = new BestStoriesService(logger.Object, hackerNewsClient.Object);
        
        // act
        var result = await service.GetBestStoriesAsync(10, CancellationToken.None);

        // assert
        Assert.Equal(BestStoriesStatus.HackerNewsServiceError, result.Status);
        Assert.Empty(result.Stories);
        hackerNewsClient.Verify(client => client.GetBestStoriesIds(It.IsAny<CancellationToken>()), Times.Once);
        hackerNewsClient.Verify(client => client.GetStory(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task When_HackerNewsClientGetStoryThrowsException_Expect_HackerNewsServiceError()
    {
        // arrange
        var logger = new Mock<ILogger<BestStoriesService>>();

        var hackerNewsClient = new Mock<IHackerNewsClient>();
        hackerNewsClient
            .Setup(client => client.GetBestStoriesIds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => [3, 1, 2]);
            
        hackerNewsClient
            .Setup(client => client.GetStory(It.IsAny<int>(), It.IsAny<CancellationToken>()))     
            .Throws<TimeoutException>();
        
        var service = new BestStoriesService(logger.Object, hackerNewsClient.Object);
        
        // act
        var result = await service.GetBestStoriesAsync(10, CancellationToken.None);

        // assert
        Assert.Equal(BestStoriesStatus.HackerNewsServiceError, result.Status);
        Assert.Empty(result.Stories);
        hackerNewsClient.Verify(client => client.GetBestStoriesIds(It.IsAny<CancellationToken>()), Times.Once);
        hackerNewsClient.Verify(client => client.GetStory(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.AtLeast(1));
    }
    
    [Theory]
    [InlineData(new int[] { 3, 1, 2})]
    [InlineData(new int[] { 3, 2, 1})]
    public async Task When_ThereAreLessBestStoriesThanLimit_Expect_AllAvailableStoriesSortedByScoreDescending(int[] ids)
    {
        // arrange
        var logger = new Mock<ILogger<BestStoriesService>>();
        
        var hackerNewsClient = new Mock<IHackerNewsClient>();
        hackerNewsClient
            .Setup(client => client.GetBestStoriesIds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ids);
        
        var now = DateTime.UtcNow;
        HackerNewsStoryDto[] storiesDto = [
            new HackerNewsStoryDto()
            {
                Id = 1,
                By = "author1",
                Descendants = 101,
                Kids = [1001, 1002, 1003],
                Score = 1000,
                Time = now.AddDays(-10),
                Title = "title1",
                Type = "type1",
                Url = "url1"
            },
            new HackerNewsStoryDto()
            {
                Id = 2,
                By = "author2",
                Descendants = 202,
                Kids = [2001, 2002, 2003],
                Score = 2000,
                Time = now.AddDays(-20),
                Title = "title2",
                Type = "type2",
                Url = "url2"
            },
            new HackerNewsStoryDto()
            {
                Id = 3,
                By = "author3",
                Descendants = 303,
                Kids = [3001, 3002, 3003],
                Score = 3000,
                Time = now.AddDays(-30),
                Title = "title3",
                Type = "type3",
                Url = "url3"
            }
        ]; 
        
        hackerNewsClient
            .Setup(client => client.GetStory(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => storiesDto[0]);
        hackerNewsClient
            .Setup(client => client.GetStory(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => storiesDto[1]);
        hackerNewsClient
            .Setup(client => client.GetStory(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => storiesDto[2]);
        
        var service = new BestStoriesService(logger.Object, hackerNewsClient.Object);
        
        // act
        var result = await service.GetBestStoriesAsync(10, CancellationToken.None);

        // assert
        Assert.Equal(BestStoriesStatus.Ok, result.Status);
        Story[] expected = [
            new Story()
            {
                Title = storiesDto[2].Title,
                Uri = storiesDto[2].Url,
                PostedBy = storiesDto[2].By,
                Time = storiesDto[2].Time,
                Score = storiesDto[2].Score,
                CommentCount = storiesDto[2].Descendants,
            },
            new Story()
            {
                Title = storiesDto[1].Title,
                Uri = storiesDto[1].Url,
                PostedBy = storiesDto[1].By,
                Time = storiesDto[1].Time,
                Score = storiesDto[1].Score,
                CommentCount = storiesDto[1].Descendants,
            },
            new Story()
            {
                Title = storiesDto[0].Title,
                Uri = storiesDto[0].Url,
                PostedBy = storiesDto[0].By,
                Time = storiesDto[0].Time,
                Score = storiesDto[0].Score,
                CommentCount = storiesDto[0].Descendants,
            }
        ];
        Assert.Equal(expected, result.Stories);
        hackerNewsClient.Verify(client => client.GetBestStoriesIds(It.IsAny<CancellationToken>()), Times.Once);
        hackerNewsClient.Verify(client => client.GetStory(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }
    
    
    [Fact]
    public async Task When_ThereAreMoreBestStoriesThanLimit_Expect_LimitSortedByScoreDescending()
    {
        // arrange
        var logger = new Mock<ILogger<BestStoriesService>>();
        
        var hackerNewsClient = new Mock<IHackerNewsClient>();
        hackerNewsClient
            .Setup(client => client.GetBestStoriesIds(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => [3, 2, 1]);
        
        var now = DateTime.UtcNow;
        HackerNewsStoryDto[] storiesDto = [
            new HackerNewsStoryDto()
            {
                Id = 1,
                By = "author1",
                Descendants = 101,
                Kids = [1001, 1002, 1003],
                Score = 1000,
                Time = now.AddDays(-10),
                Title = "title1",
                Type = "type1",
                Url = "url1"
            },
            new HackerNewsStoryDto()
            {
                Id = 2,
                By = "author2",
                Descendants = 202,
                Kids = [2001, 2002, 2003],
                Score = 2000,
                Time = now.AddDays(-20),
                Title = "title2",
                Type = "type2",
                Url = "url2"
            },
            new HackerNewsStoryDto()
            {
                Id = 3,
                By = "author3",
                Descendants = 303,
                Kids = [3001, 3002, 3003],
                Score = 3000,
                Time = now.AddDays(-30),
                Title = "title3",
                Type = "type3",
                Url = "url3"
            }
        ]; 
        
        hackerNewsClient
            .Setup(client => client.GetStory(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => storiesDto[0]);
        hackerNewsClient
            .Setup(client => client.GetStory(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => storiesDto[1]);
        hackerNewsClient
            .Setup(client => client.GetStory(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => storiesDto[2]);
        
        var service = new BestStoriesService(logger.Object, hackerNewsClient.Object);
        
        // act
        var result = await service.GetBestStoriesAsync(1, CancellationToken.None);

        // assert
        Assert.Equal(BestStoriesStatus.Ok, result.Status);
        Story[] expected = [
            new Story()
            {
                Title = storiesDto[2].Title,
                Uri = storiesDto[2].Url,
                PostedBy = storiesDto[2].By,
                Time = storiesDto[2].Time,
                Score = storiesDto[2].Score,
                CommentCount = storiesDto[2].Descendants
            }
        ];
        Assert.Equal(expected, result.Stories);
        hackerNewsClient.Verify(client => client.GetBestStoriesIds(It.IsAny<CancellationToken>()), Times.Once);
        hackerNewsClient.Verify(client => client.GetStory(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}