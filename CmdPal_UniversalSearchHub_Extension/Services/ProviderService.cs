// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using CmdPal_UniversalSearchHub_Extension.Models;

namespace CmdPal_UniversalSearchHub_Extension.Services;

internal sealed class ProviderService
{
    private const string DataFolderName = "CmdPal_UniversalSearchHub";
    private const string ProvidersFileName = "providers.json";

    private static readonly Lazy<ProviderService> LazyInstance = new(() => new ProviderService());

    private readonly string _providersPath;
    private readonly Lock _sync = new();

    internal static ProviderService Instance => LazyInstance.Value;

    internal static event EventHandler? ProvidersChanged;

    private ProviderService()
    {
        string root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            DataFolderName);
        _providersPath = Path.Combine(root, ProvidersFileName);
    }

    internal string ProvidersFilePath => _providersPath;

    internal IReadOnlyList<SearchProvider> LoadProviders()
    {
        lock (_sync)
        {
            ProvidersDocument doc = LoadOrMigrateDocument();
            return doc.Providers.Select(BuiltInProviderCatalog.Clone).ToList();
        }
    }

    internal IReadOnlyList<SearchProvider> LoadEnabledProviders() =>
        LoadProviders().Where(static p => p.Enabled).ToList();

    internal SearchProvider? GetById(string id)
    {
        lock (_sync)
        {
            ProvidersDocument doc = LoadOrMigrateDocument();
            SearchProvider? p = doc.Providers.FirstOrDefault(x => x.Id.Equals(id, StringComparison.Ordinal));
            return p == null ? null : BuiltInProviderCatalog.Clone(p);
        }
    }

    internal void ToggleEnabled(string id)
    {
        lock (_sync)
        {
            ProvidersDocument doc = LoadOrMigrateDocument();
            SearchProvider? p = doc.Providers.FirstOrDefault(x => x.Id.Equals(id, StringComparison.Ordinal));
            if (p != null)
            {
                p.Enabled = !p.Enabled;
                WriteDocument(doc);
            }
        }

        NotifyChanged();
    }

    internal void Remove(string id)
    {
        lock (_sync)
        {
            ProvidersDocument doc = LoadOrMigrateDocument();
            int removed = doc.Providers.RemoveAll(x => x.Id.Equals(id, StringComparison.Ordinal) && !x.IsBuiltIn);
            if (removed > 0)
            {
                WriteDocument(doc);
            }
        }

        NotifyChanged();
    }

    internal bool TryUpsert(SearchProvider provider, string? editingId, out string? errorMessage)
    {
        errorMessage = null;
        if (!ValidateFields(provider, out errorMessage))
        {
            return false;
        }

        lock (_sync)
        {
            ProvidersDocument doc = LoadOrMigrateDocument();
            if (!IsPrefixUnique(doc.Providers, provider.Prefix, editingId, out errorMessage))
            {
                return false;
            }

            if (string.IsNullOrEmpty(editingId))
            {
                provider.Id = "user-" + Guid.NewGuid().ToString("N");
                provider.IsBuiltIn = false;
                provider.Enabled = true;
                doc.Providers.Add(provider);
            }
            else
            {
                SearchProvider? existing = doc.Providers.FirstOrDefault(x => x.Id.Equals(editingId, StringComparison.Ordinal));
                if (existing == null)
                {
                    errorMessage = "Provider not found.";
                    return false;
                }

                existing.Name = provider.Name;
                existing.Prefix = provider.Prefix.Trim();
                existing.BaseUrl = provider.BaseUrl.Trim();
                if (!existing.IsBuiltIn)
                {
                    existing.Enabled = provider.Enabled;
                }
            }

            WriteDocument(doc);
        }

        NotifyChanged();
        return true;
    }

    internal static bool ValidateFields(SearchProvider provider, out string? errorMessage)
    {
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(provider.Name))
        {
            errorMessage = "Name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(provider.Prefix))
        {
            errorMessage = "Prefix is required.";
            return false;
        }

        if (provider.Prefix.Contains(' ', StringComparison.Ordinal))
        {
            errorMessage = "Prefix cannot contain spaces.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(provider.BaseUrl))
        {
            errorMessage = "URL template is required.";
            return false;
        }

        if (!provider.BaseUrl.Contains("{0}", StringComparison.Ordinal))
        {
            errorMessage = "URL template must include {0} for the search query.";
            return false;
        }

        try
        {
            string sample = string.Format(System.Globalization.CultureInfo.InvariantCulture, provider.BaseUrl, "q");
            if (!Uri.TryCreate(sample, UriKind.Absolute, out _))
            {
                errorMessage = "URL template does not form a valid URL with a sample query.";
                return false;
            }
        }
        catch (FormatException)
        {
            errorMessage = "URL template has invalid brace placeholders; use exactly {0} for the query.";
            return false;
        }

        return true;
    }

    private static bool IsPrefixUnique(
        List<SearchProvider> list,
        string prefix,
        string? editingId,
        out string? errorMessage)
    {
        errorMessage = null;
        string trimmed = prefix.Trim();
        foreach (SearchProvider p in list)
        {
            if (editingId != null && p.Id.Equals(editingId, StringComparison.Ordinal))
            {
                continue;
            }

            if (p.Prefix.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = $"Another provider already uses prefix '{p.Prefix}'.";
                return false;
            }
        }

        return true;
    }

    private ProvidersDocument LoadOrMigrateDocument()
    {
        string? dir = Path.GetDirectoryName(_providersPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (!File.Exists(_providersPath))
        {
            var fresh = new ProvidersDocument
            {
                Version = 2,
                Providers = BuiltInProviderCatalog.CreateFreshDocumentList(),
            };
            WriteDocument(fresh);
            return fresh;
        }

        string json = File.ReadAllText(_providersPath);
        string trimmed = json.TrimStart();
        bool dirty = false;

        if (trimmed.StartsWith('['))
        {
            ProvidersDocument migrated = MigrateFromLegacyArray(json);
            WriteDocument(migrated);
            return migrated;
        }

        ProvidersDocument? doc;
        try
        {
            doc = JsonSerializer.Deserialize(json, ProviderJsonContext.Default.ProvidersDocument);
        }
        catch (JsonException)
        {
            doc = null;
        }

        if (doc?.Providers is not { Count: > 0 })
        {
            doc = new ProvidersDocument
            {
                Version = 2,
                Providers = BuiltInProviderCatalog.CreateFreshDocumentList(),
            };
            WriteDocument(doc);
            return doc;
        }

        doc.Version = 2;
        dirty |= NormalizeIds(doc.Providers);
        dirty |= MergeMissingBuiltIns(doc.Providers);
        if (dirty)
        {
            WriteDocument(doc);
        }

        return doc;
    }

    private static ProvidersDocument MigrateFromLegacyArray(string json)
    {
        List<SearchProvider>? list = JsonSerializer.Deserialize(json, ProviderJsonContext.Default.ListSearchProvider);
        list ??= [];
        foreach (SearchProvider p in list)
        {
            if (string.IsNullOrEmpty(p.Id))
            {
                SearchProvider? match = BuiltInProviderCatalog.FindMatchByPrefixAndName(p.Prefix, p.Name)
                    ?? BuiltInProviderCatalog.FindByPrefixIgnoreCase(p.Prefix);
                if (match != null)
                {
                    p.Id = match.Id;
                    p.IsBuiltIn = true;
                    if (!p.BaseUrl.Contains("{0}", StringComparison.Ordinal))
                    {
                        p.BaseUrl = match.BaseUrl;
                    }
                }
                else
                {
                    p.Id = "user-" + Guid.NewGuid().ToString("N");
                    p.IsBuiltIn = false;
                }
            }

            if (string.IsNullOrEmpty(p.BaseUrl) || !p.BaseUrl.Contains("{0}", StringComparison.Ordinal))
            {
                SearchProvider? match = BuiltInProviderCatalog.FindById(p.Id);
                if (match != null)
                {
                    p.BaseUrl = match.BaseUrl;
                }
            }
        }

        MergeMissingBuiltIns(list);
        return new ProvidersDocument { Version = 2, Providers = list };
    }

    private static bool NormalizeIds(List<SearchProvider> list)
    {
        bool changed = false;
        foreach (SearchProvider p in list)
        {
            if (!string.IsNullOrEmpty(p.Id))
            {
                continue;
            }

            SearchProvider? match = BuiltInProviderCatalog.FindMatchByPrefixAndName(p.Prefix, p.Name)
                ?? BuiltInProviderCatalog.FindByPrefixIgnoreCase(p.Prefix);
            if (match != null)
            {
                p.Id = match.Id;
                p.IsBuiltIn = true;
            }
            else
            {
                p.Id = "user-" + Guid.NewGuid().ToString("N");
                p.IsBuiltIn = false;
            }

            changed = true;
        }

        return changed;
    }

    private static bool MergeMissingBuiltIns(List<SearchProvider> list)
    {
        var existingIds = new HashSet<string>(list.Select(p => p.Id), StringComparer.Ordinal);
        bool added = false;
        foreach (SearchProvider builtin in BuiltInProviderCatalog.Defaults)
        {
            if (!existingIds.Contains(builtin.Id))
            {
                list.Add(BuiltInProviderCatalog.Clone(builtin));
                added = true;
            }
        }

        return added;
    }

    private void WriteDocument(ProvidersDocument doc)
    {
        doc.Version = 2;
        string outJson = JsonSerializer.Serialize(doc, ProviderJsonContext.Default.ProvidersDocument);
        File.WriteAllText(_providersPath, outJson);
    }

    private static void NotifyChanged() => ProvidersChanged?.Invoke(null, EventArgs.Empty);
}
