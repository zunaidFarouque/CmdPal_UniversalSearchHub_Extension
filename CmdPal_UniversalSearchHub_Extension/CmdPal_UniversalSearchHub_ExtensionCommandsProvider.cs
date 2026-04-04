// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CmdPal_UniversalSearchHub_Extension.Pages;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace CmdPal_UniversalSearchHub_Extension;

public partial class CmdPal_UniversalSearchHub_ExtensionCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public CmdPal_UniversalSearchHub_ExtensionCommandsProvider()
    {
        DisplayName = "CmdPal - Universal Search Hub";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        _commands = [
            new CommandItem(new CmdPal_UniversalSearchHub_ExtensionPage()) { Title = DisplayName },
            new CommandItem(new ProviderSettingsPage())
            {
                Title = "Configure search providers",
                Subtitle = "Enable, add, or remove engines",
            },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

}
