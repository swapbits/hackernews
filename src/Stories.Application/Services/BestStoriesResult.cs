using System.Diagnostics.CodeAnalysis;
using Stories.Application.Dto;

namespace Stories.Application.Services;

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