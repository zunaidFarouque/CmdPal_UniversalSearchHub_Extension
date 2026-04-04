// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json;
using CmdPal_UniversalSearchHub_Extension.Models;

namespace CmdPal_UniversalSearchHub_Extension.Services;

internal sealed class ProviderService
{
    private const string DataFolderName = "CmdPal_UniversalSearchHub";
    private const string ProvidersFileName = "providers.json";

    private static readonly SearchProvider[] DefaultProviders =
    [
        new()
        {
            Name = "Google",
            Prefix = "g",
            BaseUrl = "https://www.google.com/search?q={0}",
        },
        new()
        {
            Name = "YouTube",
            Prefix = "yt",
            BaseUrl = "https://www.youtube.com/results?search_query={0}",
        },
        new()
        {
            Name = "GitHub",
            Prefix = "gh",
            BaseUrl = "https://github.com/search?q={0}",
        },
    ];

    private readonly string _providersPath;

    internal ProviderService()
    {
        string root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            DataFolderName);
        _providersPath = Path.Combine(root, ProvidersFileName);
    }

    internal IReadOnlyList<SearchProvider> LoadProviders()
    {
        EnsureProvidersFileExists();

        string json = File.ReadAllText(_providersPath);
        try
        {
            List<SearchProvider>? list = JsonSerializer.Deserialize(json, ProviderJsonContext.Default.ListSearchProvider);
            if (list is { Count: > 0 })
            {
                return list;
            }
        }
        catch (JsonException)
        {
            // Malformed user file — fall back to built-in defaults for this session.
        }

        return [.. DefaultProviders];
    }

    private void EnsureProvidersFileExists()
    {
        string? dir = Path.GetDirectoryName(_providersPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (File.Exists(_providersPath))
        {
            return;
        }

        string json = JsonSerializer.Serialize(DefaultProviders, ProviderJsonContext.Default.SearchProviderArray);
        File.WriteAllText(_providersPath, json);
    }
}
