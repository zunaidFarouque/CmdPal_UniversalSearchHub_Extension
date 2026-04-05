// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Nodes;
using CmdPal_UniversalSearchHub_Extension.Models;
using CmdPal_UniversalSearchHub_Extension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace CmdPal_UniversalSearchHub_Extension.Pages;

internal sealed partial class ProviderFormPage : ContentPage
{
    private readonly ProviderFormContent _form;

    public ProviderFormPage(string? editingProviderId)
    {
        Icon = new IconInfo("\uE710");
        Title = string.IsNullOrEmpty(editingProviderId) ? "Add custom provider" : "Edit provider";
        Name = "Open";
        _form = new ProviderFormContent(editingProviderId);
    }

    public override IContent[] GetContent() => [_form];
}

internal sealed partial class ProviderFormContent : FormContent
{
    private readonly string? _editingId;

    public ProviderFormContent(string? editingProviderId)
    {
        _editingId = editingProviderId;
        SearchProvider? existing = string.IsNullOrEmpty(editingProviderId)
            ? null
            : ProviderService.Instance.GetById(editingProviderId);

        string nameJson = JsonSerializer.Serialize(existing?.Name ?? "", ProviderJsonContext.Default.String);
        string prefixJson = JsonSerializer.Serialize(existing?.Prefix ?? "", ProviderJsonContext.Default.String);
        string baseUrlJson = JsonSerializer.Serialize(existing?.BaseUrl ?? "", ProviderJsonContext.Default.String);
        string hintJson = JsonSerializer.Serialize(
            "URL template must include {0} once for the encoded search query.",
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
                        "text": {{hintJson}},
                        "wrap": true
                    },
                    {
                        "type": "Input.Text",
                        "id": "Name",
                        "label": "Display name",
                        "style": "text",
                        "isRequired": true,
                        "errorMessage": "Name is required",
                        "placeholder": "e.g. My Search",
                        "value": {{nameJson}}
                    },
                    {
                        "type": "Input.Text",
                        "id": "Prefix",
                        "label": "Abbreviation (no spaces)",
                        "style": "text",
                        "isRequired": true,
                        "errorMessage": "Abbreviation is required",
                        "placeholder": "e.g. g or wiki",
                        "value": {{prefixJson}}
                    },
                    {
                        "type": "Input.Text",
                        "id": "BaseUrl",
                        "label": "URL template",
                        "style": "url",
                        "isRequired": true,
                        "errorMessage": "URL template is required",
                        "placeholder": "https://example.com/search?q={0}",
                        "value": {{baseUrlJson}}
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

        string? name = GetField(root, "Name", "name");
        string? prefix = GetField(root, "Prefix", "prefix");
        string? baseUrl = GetField(root, "BaseUrl", "baseUrl", "baseurl");

        var provider = new SearchProvider
        {
            Name = name?.Trim() ?? "",
            Prefix = prefix?.Trim() ?? "",
            BaseUrl = baseUrl?.Trim() ?? "",
        };

        if (!ProviderService.Instance.TryUpsert(provider, _editingId, out string? error))
        {
            var toast = new ToastStatusMessage(error ?? "Could not save provider.");
            toast.Show();
            return CommandResult.KeepOpen();
        }

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
