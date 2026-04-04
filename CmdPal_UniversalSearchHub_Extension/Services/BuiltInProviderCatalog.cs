// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CmdPal_UniversalSearchHub_Extension.Models;

namespace CmdPal_UniversalSearchHub_Extension.Services;

internal static class BuiltInProviderCatalog
{
    private static readonly SearchProvider[] All =
    [
        B("builtin.google", "Google", "g", "https://www.google.com/search?q={0}", true),
        B("builtin.youtube", "YouTube", "yt", "https://www.youtube.com/results?search_query={0}", true),
        B("builtin.github", "GitHub", "gh", "https://github.com/search?q={0}&type=repositories", true),
        B("builtin.bing", "Bing", "b", "https://www.bing.com/search?q={0}", true),
        B("builtin.duckduckgo", "DuckDuckGo", "ddg", "https://duckduckgo.com/?q={0}", true),
        B("builtin.wikipedia", "Wikipedia", "wiki", "https://en.wikipedia.org/wiki/Special:Search?search={0}", false),
        B("builtin.stackoverflow", "Stack Overflow", "so", "https://stackoverflow.com/search?q={0}", false),
        B("builtin.reddit", "Reddit", "reddit", "https://www.reddit.com/search/?q={0}", false),
        B("builtin.x", "X (Twitter)", "x", "https://twitter.com/search?q={0}", false),
        B("builtin.amazon", "Amazon", "amazon", "https://www.amazon.com/s?k={0}", false),
        B("builtin.imdb", "IMDb", "imdb", "https://www.imdb.com/find?q={0}", false),
        B("builtin.maps", "Google Maps", "maps", "https://www.google.com/maps/search/{0}", false),
        B("builtin.netflix", "Netflix", "nf", "https://www.netflix.com/search?q={0}", false),
        B("builtin.spotify", "Spotify", "sp", "https://open.spotify.com/search/{0}", false),
        B("builtin.mdn", "MDN Web Docs", "mdn", "https://developer.mozilla.org/en-US/search?q={0}", false),
        B("builtin.npm", "npm", "npm", "https://www.npmjs.com/search?q={0}", false),
        B("builtin.pypi", "PyPI", "pypi", "https://pypi.org/search/?q={0}", false),
        B("builtin.crates", "crates.io", "crates", "https://crates.io/search?q={0}", false),
        B("builtin.dockerhub", "Docker Hub", "dh", "https://hub.docker.com/search?q={0}", false),
        B("builtin.translate", "Google Translate", "tr", "https://translate.google.com/?sl=auto&tl=en&text={0}", false),
        B("builtin.scholar", "Google Scholar", "scholar", "https://scholar.google.com/scholar?q={0}", false),
        B("builtin.news", "Google News", "news", "https://news.google.com/search?q={0}&hl=en&gl=US&ceid=US:en", false),
        B("builtin.brave", "Brave Search", "brave", "https://search.brave.com/search?q={0}", false),
        B("builtin.wolfram", "Wolfram|Alpha", "wa", "https://www.wolframalpha.com/input?i={0}", false),
        B("builtin.archive", "Internet Archive", "ia", "https://archive.org/search?query={0}", false),
    ];

    internal static IReadOnlyList<SearchProvider> Defaults => All;

    internal static SearchProvider Clone(SearchProvider p) =>
        new()
        {
            Id = p.Id,
            Name = p.Name,
            Prefix = p.Prefix,
            BaseUrl = p.BaseUrl,
            Enabled = p.Enabled,
            IsBuiltIn = p.IsBuiltIn,
        };

    internal static List<SearchProvider> CreateFreshDocumentList() =>
        All.Select(Clone).ToList();

    internal static SearchProvider? FindById(string id)
    {
        foreach (SearchProvider p in All)
        {
            if (p.Id.Equals(id, StringComparison.Ordinal))
            {
                return p;
            }
        }

        return null;
    }

    internal static SearchProvider? FindMatchByPrefixAndName(string prefix, string name)
    {
        foreach (SearchProvider p in All)
        {
            if (p.Prefix.Equals(prefix, StringComparison.OrdinalIgnoreCase)
                && p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return p;
            }
        }

        return null;
    }

    internal static SearchProvider? FindByPrefixIgnoreCase(string prefix)
    {
        foreach (SearchProvider p in All)
        {
            if (p.Prefix.Equals(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return p;
            }
        }

        return null;
    }

    private static SearchProvider B(string id, string name, string prefix, string baseUrl, bool enabled) =>
        new()
        {
            Id = id,
            Name = name,
            Prefix = prefix,
            BaseUrl = baseUrl,
            Enabled = enabled,
            IsBuiltIn = true,
        };
}
