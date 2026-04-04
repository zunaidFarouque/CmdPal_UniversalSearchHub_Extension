// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using CmdPal_UniversalSearchHub_Extension.Commands;
using CmdPal_UniversalSearchHub_Extension.Models;
using CmdPal_UniversalSearchHub_Extension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace CmdPal_UniversalSearchHub_Extension;

internal sealed partial class CmdPal_UniversalSearchHub_ExtensionPage : DynamicListPage
{
    private static readonly IconInfo SearchIcon = new("\uE721");

    private readonly ProviderService _providers = new();

    public CmdPal_UniversalSearchHub_ExtensionPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "CmdPal - Universal Search Hub";
        Name = "Open";
        PlaceholderText = "Type g query, yt query, gh query, or pick a provider…";
    }

    public override void UpdateSearchText(string oldSearch, string newSearch) => RaiseItemsChanged();

    public override IListItem[] GetItems()
    {
        IReadOnlyList<SearchProvider> providers = _providers.LoadProviders();
        string text = SearchText.Trim();

        if (TryGetPrefixQuery(text, providers, out SearchProvider? matched, out string? query))
        {
            string url = BuildSearchUrl(matched!, query!);
            return
            [
                new ListItem(new SearchUrlCommand(url))
                {
                    Title = $"Search {matched!.Name} for '{query}'",
                    Subtitle = url,
                    Icon = SearchIcon,
                },
            ];
        }

        var items = new List<IListItem>();
        foreach (SearchProvider p in providers)
        {
            if (string.IsNullOrEmpty(text))
            {
                items.Add(new ListItem(new NoOpCommand())
                {
                    Title = p.Name,
                    Subtitle = $"Search on {p.Name}",
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
        string.Format(provider.BaseUrl, Uri.EscapeDataString(rawQuery));
}
