// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CmdPal_UniversalSearchHub_Extension.Models;
using CmdPal_UniversalSearchHub_Extension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace CmdPal_UniversalSearchHub_Extension.Pages;

internal sealed partial class ProviderSettingsPage : ListPage
{
    public ProviderSettingsPage()
    {
        Icon = new IconInfo("\uE713");
        Title = "Configure search providers";
        Name = "Open";
    }

    public override IListItem[] GetItems()
    {
        IReadOnlyList<SearchProvider> providers = ProviderService.Instance.LoadProviders()
            .OrderBy(static p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var items = new List<IListItem>
        {
            new ListItem(new ProviderHelpPage())
            {
                Title = "Help and data file location",
                Subtitle = "Markdown",
                Icon = new IconInfo("\uE946"),
            },
            new ListItem(new ProviderFormPage(null))
            {
                Title = "Add custom provider",
                Subtitle = "Name, prefix, URL template with {0}",
                Icon = new IconInfo("\uE710"),
            },
        };

        foreach (SearchProvider p in providers)
        {
            string on = p.Enabled ? "On" : "Off";
            string kind = p.IsBuiltIn ? "Built-in" : "Custom";
            items.Add(new ListItem(new ProviderDetailPage(p.Id))
            {
                Title = p.Name,
                Subtitle = $"{p.Prefix} · {on} · {kind}",
                Icon = new IconInfo("\uE721"),
            });
        }

        return items.ToArray();
    }
}
