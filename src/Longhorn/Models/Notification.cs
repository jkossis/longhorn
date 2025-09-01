using System.Text.Json.Serialization;

namespace Longhorn.Models;

public record Notification
{
    [JsonPropertyName("title")]
    public required string Title { get; init; }
    [JsonPropertyName("body")]
    public required string Body { get; init; }
}