using System.Text.Json.Serialization;

namespace CmdPal_UniversalSearchHub_Extension.Services;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(YouTubeSearchListResponse))]
[JsonSerializable(typeof(GoogleCustomSearchResponse))]
internal sealed partial class SearchApiJsonContext : JsonSerializerContext;

internal sealed class YouTubeSearchListResponse
{
    public YouTubeSearchItem[]? Items { get; set; }
}

internal sealed class YouTubeSearchItem
{
    public YouTubeSearchId? Id { get; set; }

    public YouTubeSearchSnippet? Snippet { get; set; }
}

internal sealed class YouTubeSearchId
{
    public string? VideoId { get; set; }
}

internal sealed class YouTubeSearchSnippet
{
    public string? Title { get; set; }

    public string? ChannelTitle { get; set; }
}

internal sealed class GoogleCustomSearchResponse
{
    public GoogleCustomSearchItem[]? Items { get; set; }
}

internal sealed class GoogleCustomSearchItem
{
    public string? Title { get; set; }

    public string? Link { get; set; }

    public string? Snippet { get; set; }
}