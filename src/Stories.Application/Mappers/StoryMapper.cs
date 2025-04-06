using Stories.Application.Dto;

namespace Stories.Application.Mappers;

public static class StoryMapper
{
    public static Story Map(HackerNewsStoryDto store) =>
        new()
        {
            Title = store.Title,
            Uri = store.Url,
            PostedBy = store.By,
            Time = store.Time,
            Score = store.Score,
            CommentCount = store.Descendants
        };
}