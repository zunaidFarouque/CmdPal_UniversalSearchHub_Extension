namespace CmdPal_UniversalSearchHub_Extension.Models;

internal sealed class HubSettings
{
    public bool EnableQuerySuggestions { get; set; }

    public bool EnableResultPreview { get; set; }

    public string YouTubeDataApiKey { get; set; } = "";

    public string GoogleCustomSearchApiKey { get; set; } = "";

    public string GoogleCustomSearchEngineId { get; set; } = "";

    internal static HubSettings CreateDefault() => new();

    internal static HubSettings Clone(HubSettings s) =>
        new()
        {
            EnableQuerySuggestions = s.EnableQuerySuggestions,
            EnableResultPreview = s.EnableResultPreview,
            YouTubeDataApiKey = s.YouTubeDataApiKey,
            GoogleCustomSearchApiKey = s.GoogleCustomSearchApiKey,
            GoogleCustomSearchEngineId = s.GoogleCustomSearchEngineId,
        };
}
