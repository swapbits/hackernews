using System.Text.Json.Serialization;
using Stories.Application.Converters;

namespace Stories.Application.Dto;

public class HackerNewsStoryDto 
{
    public required int Id { get; init; }
    
    public required string By { get; init; }
    
    public required int Descendants { get; init; }
    
    public required int[] Kids { get; init; }
    
    public required int Score { get; init; }
    
    [JsonConverter(typeof(UnixTimeConverter))]
    public required DateTime? Time { get; init; }
    
    public required string Title { get; init; }
    
    public string? Type { get; init; }
    
    public string? Url { get; init; }
}