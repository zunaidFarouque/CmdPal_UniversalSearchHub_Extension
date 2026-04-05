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
        ShowDetails = true;
    }

    public override IListItem[] GetItems()
    {
        try
        {
            return GetItemsCore();
        }
        catch (Exception ex)
        {
            return
            [
                new ListItem(new NoOpCommand())
                {
                    Title = "Could not load providers",
                    Subtitle = ex.Message,
                    Icon = new IconInfo("\uE783"),
                },
            ];
        }
    }

    private IListItem[] GetItemsCore()
    {
        IReadOnlyList<SearchProvider> providers = ProviderService.Instance.LoadProviders()
            .OrderBy(static p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        HubSettings hub = ProviderService.Instance.LoadHubSettings();
        string sug = hub.EnableQuerySuggestions ? "On" : "Off";
        string prev = hub.EnableResultPreview ? "On" : "Off";

        var items = new List<IListItem>
        {
            new ListItem(new ProviderHelpPage())
            {
                Title = "Help and data file location",
                Subtitle = "Markdown",
                Icon = new IconInfo("\uE946"),
            },
            new ListItem(new HubKeysFormPage())
            {
                Title = "Preview API keys",
                Subtitle = "YouTube Data API + Google Programmable Search (for in-palette preview)",
                Icon = new IconInfo("\uE72E"),
            },
            new ListItem(new HubFeatureToggleCommand(this, HubFeatureToggleCommand.Kind.QuerySuggestions))
            {
                Title = "Query suggestions (Google / YouTube)",
                Subtitle = $"{sug} · network; abbrev + space + query in the hub",
                Icon = new IconInfo("\uE721"),
            },
            new ListItem(new HubFeatureToggleCommand(this, HubFeatureToggleCommand.Kind.ResultPreview))
            {
                Title = "In-palette result preview",
                Subtitle = $"{prev} · More menu or Ctrl+Enter on a hub row (API keys above)",
                Icon = new IconInfo("\uE8A7"),
            },
            new ListItem(new ProviderFormPage(null))
            {
                Title = "Add custom provider",
                Subtitle = "Name, abbreviation, URL template with {0}",
                Icon = new IconInfo("\uE710"),
            },
        };

        foreach (SearchProvider p in providers)
        {
            string on = p.Enabled ? "On" : "Off";
            string kind = p.IsBuiltIn ? "Built-in" : "Custom";
            string subtitleUrl = TruncateMiddle(p.BaseUrl, 56);
            var editForDetails = new ProviderFormPage(p.Id)
            {
                Name = "Edit",
                Icon = new IconInfo("\uE70F"),
            };
            var editForMenu = new ProviderFormPage(p.Id)
            {
                Name = "Edit",
                Icon = new IconInfo("\uE70F"),
            };

            var toggleCmd = new SettingsToggleCommand(this, p.Id)
            {
                Name = "Toggle enabled",
                Icon = new IconInfo("\uE7E8"),
            };

            var more = new List<IContextItem>
            {
                new CommandContextItem(editForMenu)
                {
                    Title = "Edit name, abbreviation, URL",
                    Icon = new IconInfo("\uE70F"),
                },
                new CommandContextItem(toggleCmd)
                {
                    Title = p.Enabled ? "Disable" : "Enable",
                    Icon = new IconInfo("\uE7E8"),
                },
            };

            ICommand[] panelCommands = !p.IsBuiltIn
                ?
                [
                    editForDetails,
                    toggleCmd,
                    new SettingsRemoveCommand(this, p.Id)
                    {
                        Name = "Remove",
                        Icon = new IconInfo("\uE74D"),
                    },
                ]
                : [editForDetails, toggleCmd];

            if (!p.IsBuiltIn)
            {
                var removeCmd = new SettingsRemoveCommand(this, p.Id)
                {
                    Name = "Remove",
                    Icon = new IconInfo("\uE74D"),
                };
                more.Add(new CommandContextItem(removeCmd)
                {
                    Title = "Remove custom provider",
                    Icon = new IconInfo("\uE74D"),
                });
            }

            items.Add(new ListItem(new ProviderDetailPage(p.Id))
            {
                Title = p.Name,
                Subtitle = $"{p.Prefix} · {subtitleUrl} · {on} · {kind}",
                Icon = new IconInfo("\uE721"),
                Details = new Details
                {
                    Title = p.Name,
                    Body =
                        "Abbreviation is used as **abbrev + space + query** in the main hub. "
                        + "The URL template must contain **{0}** for the encoded query.",
                    Size = ContentSize.Medium,
                    Metadata =
                    [
                        new DetailsElement
                        {
                            Key = "Display name",
                            Data = new DetailsLink { Text = p.Name },
                        },
                        new DetailsElement
                        {
                            Key = "Abbreviation",
                            Data = new DetailsLink { Text = p.Prefix },
                        },
                        new DetailsElement
                        {
                            Key = "URL template",
                            Data = new DetailsLink { Text = p.BaseUrl },
                        },
                        new DetailsElement
                        {
                            Key = "Status",
                            Data = new DetailsLink { Text = $"{on} · {kind}" },
                        },
                        new DetailsElement
                        {
                            Key = "Actions",
                            Data = new DetailsCommands
                            {
                                Commands = panelCommands,
                            },
                        },
                    ],
                },
                MoreCommands = more.ToArray(),
            });
        }

        return items.ToArray();
    }

    private static string TruncateMiddle(string value, int maxLen)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLen)
        {
            return value;
        }

        int keep = maxLen - 1;
        int half = keep / 2;
        return string.Concat(value.AsSpan(0, half), "…", value.AsSpan(value.Length - (keep - half)));
    }

    private sealed partial class HubFeatureToggleCommand : InvokableCommand
    {
        internal enum Kind
        {
            QuerySuggestions,
            ResultPreview,
        }

        private readonly ProviderSettingsPage _page;
        private readonly Kind _kind;

        public HubFeatureToggleCommand(ProviderSettingsPage page, Kind kind)
        {
            _page = page;
            _kind = kind;
        }

        public override CommandResult Invoke()
        {
            HubSettings h = ProviderService.Instance.LoadHubSettings();
            if (_kind == Kind.QuerySuggestions)
            {
                ProviderService.Instance.SetEnableQuerySuggestions(!h.EnableQuerySuggestions);
            }
            else
            {
                ProviderService.Instance.SetEnableResultPreview(!h.EnableResultPreview);
            }

            _page.RaiseItemsChanged();
            return CommandResult.KeepOpen();
        }
    }

    private sealed partial class SettingsToggleCommand : InvokableCommand
    {
        private readonly ProviderSettingsPage _page;
        private readonly string _id;

        public SettingsToggleCommand(ProviderSettingsPage page, string id)
        {
            _page = page;
            _id = id;
        }

        public override CommandResult Invoke()
        {
            ProviderService.Instance.ToggleEnabled(_id);
            _page.RaiseItemsChanged();
            return CommandResult.KeepOpen();
        }
    }

    private sealed partial class SettingsRemoveCommand : InvokableCommand
    {
        private readonly ProviderSettingsPage _page;
        private readonly string _id;

        public SettingsRemoveCommand(ProviderSettingsPage page, string id)
        {
            _page = page;
            _id = id;
        }

        public override CommandResult Invoke()
        {
            var confirm = new ConfirmationArgs
            {
                Title = "Remove provider?",
                Description = "This custom provider will be removed from your configuration.",
                PrimaryCommand = new SettingsRemoveConfirmCommand(_page, _id) { Name = "Remove" },
            };
            return CommandResult.Confirm(confirm);
        }
    }

    private sealed partial class SettingsRemoveConfirmCommand : InvokableCommand
    {
        private readonly ProviderSettingsPage _page;
        private readonly string _id;

        public SettingsRemoveConfirmCommand(ProviderSettingsPage page, string id)
        {
            _page = page;
            _id = id;
        }

        public override CommandResult Invoke()
        {
            ProviderService.Instance.Remove(_id);
            _page.RaiseItemsChanged();
            return CommandResult.KeepOpen();
        }
    }
}
