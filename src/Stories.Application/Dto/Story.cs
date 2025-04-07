namespace Stories.Application.Dto;

public record Story 
{
    public required string Title { get; init; }
    public required string? Uri { get; init; }
    public required string PostedBy { get; init; }
    public required DateTime? Time { get; init; }
    public required int Score { get; init; }
    public required int CommentCount { get; init; }
}