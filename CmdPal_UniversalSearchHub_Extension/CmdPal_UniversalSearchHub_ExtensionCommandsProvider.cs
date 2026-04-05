// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CmdPal_UniversalSearchHub_Extension.Models;
using CmdPal_UniversalSearchHub_Extension.Pages;
using CmdPal_UniversalSearchHub_Extension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace CmdPal_UniversalSearchHub_Extension;

public partial class CmdPal_UniversalSearchHub_ExtensionCommandsProvider : CommandProvider
{
    private static class HostKeys
    {
        internal const string EnableQuerySuggestions = "enableQuerySuggestions";
        internal const string EnableResultPreview = "enableResultPreview";
        internal const string YouTubeDataApiKey = "youTubeDataApiKey";
        internal const string GoogleCustomSearchApiKey = "googleCustomSearchApiKey";
        internal const string GoogleCustomSearchEngineId = "googleCustomSearchEngineId";
    }

    private readonly ICommandItem[] _commands;
    private readonly Settings _hostSettings;

    public CmdPal_UniversalSearchHub_ExtensionCommandsProvider()
    {
        DisplayName = "CmdPal Search Hub";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");

        _hostSettings = BuildHostSettings();
        Settings = _hostSettings;
        _hostSettings.SettingsChanged += HostSettings_OnSettingsChanged;

        _commands = [
            new CommandItem(new CmdPal_UniversalSearchHub_ExtensionPage())
            {
                Title = DisplayName,
                Subtitle = "Multi-engine search; engine list in Configure search providers, toggles & keys in Extension settings too",
            },
            new CommandItem(new ProviderSettingsPage())
            {
                Title = "Configure search providers",
                Subtitle = "Engines, built-ins, custom URLs (full UI in CmdPal)",
            },
        ];
    }

    private static Settings BuildHostSettings()
    {
        HubSettings hub = ProviderService.Instance.LoadHubSettings();
        var s = new Settings();
        s.Add(new ToggleSetting(
            HostKeys.EnableQuerySuggestions,
            "Query suggestions (Google / YouTube)",
            "When on, shows suggestion rows for supported engines when you use abbreviation + query in the hub (network; public suggest endpoints).",
            hub.EnableQuerySuggestions));
        s.Add(new ToggleSetting(
            HostKeys.EnableResultPreview,
            "In-palette result preview",
            "When on, adds Preview in the More menu (and Ctrl+Enter when the host shows it). Rich YouTube and Google previews use the indented Preview fields below.",
            hub.EnableResultPreview));
        AddHostTextSetting(
            s,
            HostKeys.YouTubeDataApiKey,
            "Preview — YouTube Data API v3 key",
            Environment.NewLine
                + "Optional. Stored in providers.json with your other hub data. Required for rich YouTube previews.",
            hub.YouTubeDataApiKey,
            "Paste API key");
        AddHostTextSetting(
            s,
            HostKeys.GoogleCustomSearchApiKey,
            "Preview — Google Programmable Search API key",
            "Optional. Stored in providers.json. Required with the search engine ID for rich Google previews.",
            hub.GoogleCustomSearchApiKey,
            "Paste API key");
        AddHostTextSetting(
            s,
            HostKeys.GoogleCustomSearchEngineId,
            "Preview — Google Programmable Search engine ID (cx)",
            "Optional. Search engine ID paired with the Google API key above.",
            hub.GoogleCustomSearchEngineId,
            "Search engine ID (cx)");
        return s;
    }

    /// <summary>Maps to Adaptive Card Input.Text: Label → title, Description → label (supporting text only).</summary>
    private static void AddHostTextSetting(
        Settings s,
        string key,
        string title,
        string detail,
        string value,
        string placeholder)
    {
        var setting = new TextSetting(key, title, detail, value)
        {
            Placeholder = placeholder,
        };
        s.Add(setting);
    }

    private void HostSettings_OnSettingsChanged(object sender, object args)
    {
        HubSettings next = ReadHubFromHostSettings();
        ProviderService.Instance.ApplyHubSettings(next);
    }

    private HubSettings ReadHubFromHostSettings()
    {
        HubSettings hub = HubSettings.CreateDefault();
        if (_hostSettings.TryGetSetting(HostKeys.EnableQuerySuggestions, out bool sug))
        {
            hub.EnableQuerySuggestions = sug;
        }

        if (_hostSettings.TryGetSetting(HostKeys.EnableResultPreview, out bool prev))
        {
            hub.EnableResultPreview = prev;
        }

        if (_hostSettings.TryGetSetting(HostKeys.YouTubeDataApiKey, out string? yt))
        {
            hub.YouTubeDataApiKey = yt ?? "";
        }

        if (_hostSettings.TryGetSetting(HostKeys.GoogleCustomSearchApiKey, out string? gk))
        {
            hub.GoogleCustomSearchApiKey = gk ?? "";
        }

        if (_hostSettings.TryGetSetting(HostKeys.GoogleCustomSearchEngineId, out string? gcx))
        {
            hub.GoogleCustomSearchEngineId = gcx ?? "";
        }

        return hub;
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }
}
