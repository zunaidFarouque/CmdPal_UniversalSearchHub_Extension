// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CmdPal_UniversalSearchHub_Extension.Models;
using CmdPal_UniversalSearchHub_Extension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace CmdPal_UniversalSearchHub_Extension.Pages;

internal sealed partial class ProviderDetailPage : ListPage
{
    private readonly string _providerId;

    public ProviderDetailPage(string providerId)
    {
        _providerId = providerId;
        Icon = new IconInfo("\uE721");
        SearchProvider? p = ProviderService.Instance.GetById(providerId);
        Title = p?.Name ?? "Provider";
        Name = "Open";
    }

    public override IListItem[] GetItems()
    {
        SearchProvider? p = ProviderService.Instance.GetById(_providerId);
        if (p == null)
        {
            return [new ListItem(new NoOpCommand()) { Title = "Provider not found" }];
        }

        string state = p.Enabled ? "On" : "Off";
        string kind = p.IsBuiltIn ? "Built-in" : "Custom";
        var items = new List<IListItem>
        {
            new ListItem(new ToggleProviderCommand(this, _providerId))
            {
                Title = p.Enabled ? "Disable" : "Enable",
                Subtitle = $"Currently {state} · {kind} · abbreviation: {p.Prefix}",
                Icon = new IconInfo("\uE7E8"),
            },
            new ListItem(new ProviderFormPage(_providerId))
            {
                Title = "Edit name, abbreviation, or URL",
                Subtitle = p.BaseUrl,
                Icon = new IconInfo("\uE70F"),
            },
        };

        if (!p.IsBuiltIn)
        {
            items.Add(new ListItem(new DeleteProviderCommand(_providerId))
            {
                Title = "Remove custom provider",
                Subtitle = "Cannot be undone",
                Icon = new IconInfo("\uE74D"),
            });
        }

        return items.ToArray();
    }

    private sealed class ToggleProviderCommand : InvokableCommand
    {
        private readonly ProviderDetailPage _page;
        private readonly string _id;

        public ToggleProviderCommand(ProviderDetailPage page, string id)
        {
            _page = page;
            _id = id;
        }

        public override string Name => "Toggle";

        public override IconInfo Icon => new("\uE7E8");

        public override CommandResult Invoke()
        {
            ProviderService.Instance.ToggleEnabled(_id);
            _page.RaiseItemsChanged();
            SearchProvider? p = ProviderService.Instance.GetById(_id);
            if (p != null)
            {
                _page.Title = p.Name;
            }

            return CommandResult.KeepOpen();
        }
    }

    private sealed class DeleteProviderCommand : InvokableCommand
    {
        private readonly string _id;

        public DeleteProviderCommand(string id) => _id = id;

        public override string Name => "Remove";

        public override IconInfo Icon => new("\uE74D");

        public override CommandResult Invoke()
        {
            var confirm = new ConfirmationArgs
            {
                Title = "Remove provider?",
                Description = "This custom provider will be removed from your configuration.",
                PrimaryCommand = new RemoveProviderPrimaryCommand(_id) { Name = "Remove" },
            };
            return CommandResult.Confirm(confirm);
        }
    }

    private sealed class RemoveProviderPrimaryCommand : InvokableCommand
    {
        private readonly string _id;

        public RemoveProviderPrimaryCommand(string id) => _id = id;

        public override string Name => "Remove";

        public override IconInfo Icon => new("\uE74D");

        public override CommandResult Invoke()
        {
            ProviderService.Instance.Remove(_id);
            return CommandResult.GoBack();
        }
    }
}
