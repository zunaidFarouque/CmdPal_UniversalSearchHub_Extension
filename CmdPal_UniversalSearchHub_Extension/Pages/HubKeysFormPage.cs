using System.Text.Json;
using System.Text.Json.Nodes;
using CmdPal_UniversalSearchHub_Extension.Models;
using CmdPal_UniversalSearchHub_Extension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace CmdPal_UniversalSearchHub_Extension.Pages;

internal sealed partial class HubKeysFormPage : ContentPage
{
    public HubKeysFormPage()
    {
        Icon = new IconInfo("\uE72E");
        Title = "Preview API keys";
        Name = "Open";
    }

    public override IContent[] GetContent() => [new HubKeysFormContent()];
}

internal sealed partial class HubKeysFormContent : FormContent
{
    public HubKeysFormContent()
    {
        HubSettings h = ProviderService.Instance.LoadHubSettings();
        string yt = JsonSerializer.Serialize(h.YouTubeDataApiKey, ProviderJsonContext.Default.String);
        string gKey = JsonSerializer.Serialize(h.GoogleCustomSearchApiKey, ProviderJsonContext.Default.String);
        string gCx = JsonSerializer.Serialize(h.GoogleCustomSearchEngineId, ProviderJsonContext.Default.String);
        string hint = JsonSerializer.Serialize(
            "YouTube: Data API v3 key. Google: Programmable Search JSON API key and search engine ID (cx). Stored in providers.json on this PC.",
            ProviderJsonContext.Default.String);

        DataJson = "{}";

        TemplateJson =
            $$"""
            {
                "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
                "type": "AdaptiveCard",
                "version": "1.6",
                "body": [
                    {
                        "type": "TextBlock",
                        "text": {{hint}},
                        "wrap": true
                    },
                    {
                        "type": "Input.Text",
                        "id": "YouTubeDataApiKey",
                        "label": "YouTube Data API key",
                        "style": "password",
                        "placeholder": "Optional - required for YouTube video preview",
                        "value": {{yt}}
                    },
                    {
                        "type": "Input.Text",
                        "id": "GoogleCustomSearchApiKey",
                        "label": "Google Custom Search API key",
                        "style": "password",
                        "placeholder": "Optional - required for Google web preview",
                        "value": {{gKey}}
                    },
                    {
                        "type": "Input.Text",
                        "id": "GoogleCustomSearchEngineId",
                        "label": "Google Search engine ID (cx)",
                        "style": "text",
                        "placeholder": "Programmable Search Engine ID",
                        "value": {{gCx}}
                    }
                ],
                "actions": [
                    {
                        "type": "Action.Submit",
                        "title": "Save"
                    }
                ]
            }
            """;

#if DEBUG
        using (JsonDocument.Parse(TemplateJson))
        {
        }
#endif
    }

    public override CommandResult SubmitForm(string payload)
    {
        JsonObject? root = JsonNode.Parse(payload)?.AsObject();
        if (root == null)
        {
            return CommandResult.GoBack();
        }

        if (root.TryGetPropertyValue("data", out JsonNode? dataNode) && dataNode is JsonObject dataObj)
        {
            root = dataObj;
        }

        string? yt = GetField(root, "YouTubeDataApiKey", "youtubeDataApiKey");
        string? gKey = GetField(root, "GoogleCustomSearchApiKey", "googleCustomSearchApiKey");
        string? gCx = GetField(root, "GoogleCustomSearchEngineId", "googleCustomSearchEngineId");

        ProviderService.Instance.TrySaveHubApiKeys(yt ?? "", gKey ?? "", gCx ?? "", out _);
        return CommandResult.GoBack();
    }

    private static string? GetField(JsonObject root, params string[] keys)
    {
        foreach (string key in keys)
        {
            if (!root.TryGetPropertyValue(key, out JsonNode? node) || node == null)
            {
                continue;
            }

            if (node is JsonValue jv)
            {
                try
                {
                    return jv.GetValue<string>();
                }
                catch (InvalidOperationException)
                {
                    return jv.ToString();
                }
            }

            return node.ToString();
        }

        return null;
    }
}