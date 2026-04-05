using System.Collections.Generic;
using CmdPal_UniversalSearchHub_Extension.Commands;
using CmdPal_UniversalSearchHub_Extension.Models;
using CmdPal_UniversalSearchHub_Extension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace CmdPal_UniversalSearchHub_Extension.Pages;

internal sealed partial class SearchPreviewPage : ListPage
{
    private readonly SearchProvider _provider;
    private readonly string _query;
    private readonly HubSettings _hub;

    private List<IListItem>? _items;
    private string? _error;
    private bool _loadComplete;

    public SearchPreviewPage(SearchProvider provider, string query, HubSettings hub)
    {
        _provider = provider;
        _query = query;
        _hub = hub;
        Icon = new IconInfo("\uE721");
        Title = $"Preview - {provider.Name}";
        Name = "Open";
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            (IReadOnlyList<PreviewResult> results, string? err) =
                await SearchPreviewService.FetchAsync(_provider, _hub, _query, CancellationToken.None)
                    .ConfigureAwait(false);
            _error = err;
            if (results.Count > 0)
            {
                var list = new List<IListItem>(results.Count);
                foreach (PreviewResult r in results)
                {
                    list.Add(new ListItem(new SearchUrlCommand(r.Url))
                    {
                        Title = r.Title,
                        Subtitle = string.IsNullOrEmpty(r.Subtitle) ? r.Url : r.Subtitle,
                        Icon = new IconInfo("\uE8B7"),
                    });
                }

                _items = list;
            }
        }
        finally
        {
            IsLoading = false;
            _loadComplete = true;
            RaiseItemsChanged();
        }
    }

    public override IListItem[] GetItems()
    {
        if (!_loadComplete)
        {
            return
            [
                new ListItem(new NoOpCommand())
                {
                    Title = "Loading preview",
                    Subtitle = _query,
                    Icon = new IconInfo("\uE9F3"),
                },
            ];
        }

        if (_items is { Count: > 0 })
        {
            return _items.ToArray();
        }

        return
        [
            new ListItem(new NoOpCommand())
            {
                Title = "No preview",
                Subtitle = _error ?? "No results.",
                Icon = new IconInfo("\uE783"),
            },
        ];
    }
}