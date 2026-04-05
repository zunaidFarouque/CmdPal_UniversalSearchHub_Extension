using System.Collections.Generic;
using System.Globalization;
using CmdPal_UniversalSearchHub_Extension.Commands;
using CmdPal_UniversalSearchHub_Extension.Models;
using CmdPal_UniversalSearchHub_Extension.Pages;
using CmdPal_UniversalSearchHub_Extension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.System;

namespace CmdPal_UniversalSearchHub_Extension;

internal sealed partial class CmdPal_UniversalSearchHub_ExtensionPage : DynamicListPage
{
    private static readonly IconInfo SearchIcon = new("\uE721");
    private static readonly Lock SubscribeGate = new();
    private static bool _providersChangeSubscribed;
    private static WeakReference<CmdPal_UniversalSearchHub_ExtensionPage>? _activeSearchPage;

    private static readonly KeyChord PreviewShortcut = new()
    {
        Modifiers = VirtualKeyModifiers.Control,
        Vkey = (int)VirtualKey.Enter,
        ScanCode = 0,
    };

    private CancellationTokenSource? _suggestCts;
    private IReadOnlyList<string> _suggestions = [];
    private string _suggestionQuerySnapshot = "";

    public CmdPal_UniversalSearchHub_ExtensionPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "CmdPal Search Hub";
        Name = "Open";
        PlaceholderText =
            "Type g query, yt query, gh query, or pick a provider. Toggles & keys: Command Palette Extension settings or Configure search providers.";
        lock (SubscribeGate)
        {
            _activeSearchPage = new WeakReference<CmdPal_UniversalSearchHub_ExtensionPage>(this);
            if (!_providersChangeSubscribed)
            {
                ProviderService.ProvidersChanged += OnProvidersChangedStatic;
                _providersChangeSubscribed = true;
            }
        }
    }

    private static void OnProvidersChangedStatic(object? sender, EventArgs e)
    {
        lock (SubscribeGate)
        {
            if (_activeSearchPage?.TryGetTarget(out CmdPal_UniversalSearchHub_ExtensionPage? page) == true)
            {
                page.RaiseItemsChanged();
            }
        }
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        RaiseItemsChanged();
        ScheduleSuggestions(newSearch);
    }

    private void ScheduleSuggestions(string newSearch)
    {
        _suggestCts?.Cancel();
        _suggestCts?.Dispose();
        _suggestCts = null;

        HubSettings hub = ProviderService.Instance.LoadHubSettings();
        if (!hub.EnableQuerySuggestions)
        {
            _suggestions = [];
            _suggestionQuerySnapshot = "";
            return;
        }

        string text = newSearch.Trim();
        IReadOnlyList<SearchProvider> providers = ProviderService.Instance.LoadEnabledProviders();
        if (!TryGetPrefixQuery(text, providers, out SearchProvider? matched, out string? query))
        {
            _suggestions = [];
            _suggestionQuerySnapshot = "";
            return;
        }

        if (!IsSuggestCapable(matched!))
        {
            _suggestions = [];
            _suggestionQuerySnapshot = "";
            return;
        }

        string expectedSearchText = newSearch;
        var cts = new CancellationTokenSource();
        _suggestCts = cts;
        _ = RunSuggestionsAsync(matched!, query!, expectedSearchText, cts.Token);
    }

    private static bool IsSuggestCapable(SearchProvider p) =>
        p.Id.Equals("builtin.youtube", StringComparison.Ordinal)
        || p.Id.Equals("builtin.google", StringComparison.Ordinal);

    /// <summary>Host may trim search box text; compare normalized so async suggestions are not dropped.</summary>
    private bool SearchTextMatchesExpected(string expectedRaw) =>
        string.Equals(SearchText.Trim(), expectedRaw.Trim(), StringComparison.Ordinal);

    private async Task RunSuggestionsAsync(
        SearchProvider provider,
        string query,
        string expectedSearchText,
        CancellationToken ct)
    {
        try
        {
            await Task.Delay(320, ct).ConfigureAwait(false);
            IReadOnlyList<string> list = await SearchSuggestService.GetSuggestionsAsync(provider, query, ct)
                .ConfigureAwait(false);
            if (ct.IsCancellationRequested)
            {
                return;
            }

            if (!SearchTextMatchesExpected(expectedSearchText))
            {
                return;
            }

            _suggestions = list;
            _suggestionQuerySnapshot = query;
            RaiseItemsChanged();
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            if (!ct.IsCancellationRequested
                && SearchTextMatchesExpected(expectedSearchText))
            {
                _suggestions = [];
                _suggestionQuerySnapshot = "";
                RaiseItemsChanged();
            }
        }
    }

    public override IListItem[] GetItems()
    {
        IReadOnlyList<SearchProvider> providers = ProviderService.Instance.LoadEnabledProviders();
        string text = SearchText.Trim();
        HubSettings hub = ProviderService.Instance.LoadHubSettings();

        if (TryGetPrefixQuery(text, providers, out SearchProvider? matched, out string? query))
        {
            return BuildPrefixModeItems(matched!, query!, hub);
        }

        var items = new List<IListItem>();
        foreach (SearchProvider p in providers)
        {
            if (string.IsNullOrEmpty(text))
            {
                items.Add(new ListItem(new NoOpCommand())
                {
                    Title = p.Name,
                    Subtitle = $"Search on {p.Name} (type a query or use abbreviation + space)",
                    Icon = SearchIcon,
                });
            }
            else
            {
                string url = BuildSearchUrl(p, text);
                items.Add(new ListItem(new SearchUrlCommand(url))
                {
                    Title = p.Name,
                    Subtitle = url,
                    Icon = SearchIcon,
                });
            }
        }

        return items.ToArray();
    }

    private IListItem[] BuildPrefixModeItems(SearchProvider matched, string query, HubSettings hub)
    {
        var list = new List<IListItem>();
        string url = BuildSearchUrl(matched, query);
        SearchProvider matchedClone = BuiltInProviderCatalog.Clone(matched);
        HubSettings hubClone = HubSettings.Clone(hub);

        var main = new ListItem(new SearchUrlCommand(url))
        {
            Title = $"Search {matched.Name} for '{query}'",
            Subtitle = url,
            Icon = SearchIcon,
        };
        ApplyPreviewMoreCommand(main, matchedClone, query, hub, hubClone);
        list.Add(main);

        if (!hub.EnableQuerySuggestions)
        {
            list.Add(new ListItem(new NoOpCommand())
            {
                Section = "Tips",
                Title = "Query suggestions are off",
                Subtitle =
                    "Turn on in Command Palette Extension settings or Configure search providers.",
                Icon = SearchIcon,
            });
        }

        if (hub.EnableQuerySuggestions
            && _suggestions.Count > 0
            && string.Equals(query, _suggestionQuerySnapshot, StringComparison.Ordinal))
        {
            foreach (string s in _suggestions)
            {
                if (string.IsNullOrWhiteSpace(s))
                {
                    continue;
                }

                string sugUrl = BuildSearchUrl(matched, s);
                var row = new ListItem(new SearchUrlCommand(sugUrl))
                {
                    Section = "Suggestions",
                    Title = s,
                    Subtitle = sugUrl,
                    Icon = SearchIcon,
                    TextToSuggest = $"{matched.Prefix} {s}",
                };
                ApplyPreviewMoreCommand(row, matchedClone, s, hub, hubClone);
                list.Add(row);
            }
        }

        return list.ToArray();
    }

    private static void ApplyPreviewMoreCommand(
        ListItem item,
        SearchProvider providerClone,
        string queryText,
        HubSettings hub,
        HubSettings hubClone)
    {
        if (!hub.EnableResultPreview)
        {
            return;
        }

        var previewPage = new SearchPreviewPage(providerClone, queryText, hubClone);
        item.MoreCommands =
        [
            new CommandContextItem(previewPage)
            {
                Title = "Preview top results in palette",
                Icon = new IconInfo("\uE721"),
                RequestedShortcut = PreviewShortcut,
            },
        ];
    }

    private static bool TryGetPrefixQuery(
        string text,
        IReadOnlyList<SearchProvider> providers,
        out SearchProvider? matched,
        out string? query)
    {
        matched = null;
        query = null;

        int space = text.IndexOf(' ');
        if (space <= 0 || space >= text.Length - 1)
        {
            return false;
        }

        string prefix = text[..space];
        string rest = text[(space + 1)..].TrimStart();
        if (rest.Length == 0)
        {
            return false;
        }

        foreach (SearchProvider p in providers)
        {
            if (p.Prefix.Equals(prefix, StringComparison.OrdinalIgnoreCase))
            {
                matched = p;
                query = rest;
                return true;
            }
        }

        return false;
    }

    private static string BuildSearchUrl(SearchProvider provider, string rawQuery) =>
        string.Format(CultureInfo.InvariantCulture, provider.BaseUrl, Uri.EscapeDataString(rawQuery));
}