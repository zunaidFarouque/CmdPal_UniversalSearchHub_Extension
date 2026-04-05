using System.Net.Http;
using System.Text.Json;
using CmdPal_UniversalSearchHub_Extension.Models;

namespace CmdPal_UniversalSearchHub_Extension.Services;

internal static class SearchSuggestService
{
    private const int MaxSuggestions = 8;

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(8),
    };

    internal static async Task<IReadOnlyList<string>> GetSuggestionsAsync(
        SearchProvider provider,
        string query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        string q = Uri.EscapeDataString(query.Trim());
        string url = provider.Id.Equals("builtin.youtube", StringComparison.Ordinal)
            ? $"https://suggestqueries.google.com/complete/search?client=firefox&ds=yt&q={q}"
            : $"https://suggestqueries.google.com/complete/search?client=firefox&q={q}";

        using HttpResponseMessage response = await Http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return ParseSuggestBody(body);
    }

    private static List<string> ParseSuggestBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return [];
        }

        ReadOnlySpan<char> span = body.AsSpan().TrimStart();
        if (span.StartsWith(")]}'", StringComparison.Ordinal))
        {
            int nl = body.IndexOf('\n');
            if (nl >= 0 && nl + 1 < body.Length)
            {
                body = body[(nl + 1)..];
            }
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(body);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() < 2)
            {
                return [];
            }

            JsonElement arr = root[1];
            if (arr.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            List<string> list = new(MaxSuggestions);
            foreach (JsonElement el in arr.EnumerateArray())
            {
                if (list.Count >= MaxSuggestions)
                {
                    break;
                }

                string? s = el.ValueKind == JsonValueKind.String ? el.GetString() : null;
                if (!string.IsNullOrWhiteSpace(s))
                {
                    list.Add(s!);
                }
            }

            return list;
        }
        catch (JsonException)
        {
            return [];
        }
    }
}