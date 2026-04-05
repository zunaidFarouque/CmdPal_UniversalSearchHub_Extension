// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using CmdPal_UniversalSearchHub_Extension.Models;

namespace CmdPal_UniversalSearchHub_Extension.Services;

internal readonly record struct PreviewResult(string Title, string Subtitle, string Url);

internal static class SearchPreviewService
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(20),
    };

    internal static async Task<(IReadOnlyList<PreviewResult> Results, string? Error)> FetchAsync(
        SearchProvider provider,
        HubSettings hub,
        string query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return ([], "Enter a search query.");
        }

        string q = query.Trim();

        if (provider.Id.Equals("builtin.youtube", StringComparison.Ordinal))
        {
            return await FetchYouTubeAsync(hub.YouTubeDataApiKey, q, cancellationToken).ConfigureAwait(false);
        }

        if (provider.Id.Equals("builtin.google", StringComparison.Ordinal))
        {
            return await FetchGoogleAsync(
                    hub.GoogleCustomSearchApiKey,
                    hub.GoogleCustomSearchEngineId,
                    q,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        string fallbackUrl = string.Format(CultureInfo.InvariantCulture, provider.BaseUrl, Uri.EscapeDataString(q));
        return ([new PreviewResult("Open search", provider.Name, fallbackUrl)], null);
    }

    private static async Task<(IReadOnlyList<PreviewResult> Results, string? Error)> FetchYouTubeAsync(
        string apiKey,
        string query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return ([],
                "Add a YouTube Data API key (Command Palette Extension settings or Configure search providers -> Preview API keys).");
        }

        string url =
            "https://www.googleapis.com/youtube/v3/search?part=snippet&type=video&maxResults=10&q="
            + Uri.EscapeDataString(query)
            + "&key="
            + Uri.EscapeDataString(apiKey);

        try
        {
            string json = await Http.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
            YouTubeSearchListResponse? parsed =
                JsonSerializer.Deserialize(json, SearchApiJsonContext.Default.YouTubeSearchListResponse);
            if (parsed?.Items is not { Length: > 0 })
            {
                return ([], "No results.");
            }

            var list = new List<PreviewResult>(parsed.Items.Length);
            foreach (YouTubeSearchItem item in parsed.Items)
            {
                if (string.IsNullOrEmpty(item.Id?.VideoId))
                {
                    continue;
                }

                string title = item.Snippet?.Title ?? item.Id.VideoId;
                string sub = item.Snippet?.ChannelTitle ?? "YouTube";
                list.Add(new PreviewResult(title, sub, $"https://www.youtube.com/watch?v={item.Id.VideoId}"));
            }

            return list.Count == 0 ? ([], "No playable videos in results.") : (list, null);
        }
        catch (Exception ex)
        {
            return ([], ex.Message);
        }
    }

    private static async Task<(IReadOnlyList<PreviewResult> Results, string? Error)> FetchGoogleAsync(
        string apiKey,
        string cx,
        string query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(cx))
        {
            return ([],
                "Add Google Programmable Search API key and Search engine ID (cx) (Extension settings or Configure search providers -> Preview API keys).");
        }

        string url =
            "https://www.googleapis.com/customsearch/v1?key="
            + Uri.EscapeDataString(apiKey)
            + "&cx="
            + Uri.EscapeDataString(cx)
            + "&q="
            + Uri.EscapeDataString(query);

        try
        {
            string json = await Http.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
            GoogleCustomSearchResponse? parsed =
                JsonSerializer.Deserialize(json, SearchApiJsonContext.Default.GoogleCustomSearchResponse);
            if (parsed?.Items is not { Length: > 0 })
            {
                return ([], "No results.");
            }

            var list = new List<PreviewResult>(parsed.Items.Length);
            foreach (GoogleCustomSearchItem item in parsed.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Link))
                {
                    continue;
                }

                string title = item.Title ?? item.Link;
                string sub = item.Snippet ?? "";
                list.Add(new PreviewResult(title, sub, item.Link));
            }

            return list.Count == 0 ? ([], "No links in results.") : (list, null);
        }
        catch (Exception ex)
        {
            return ([], ex.Message);
        }
    }
}
