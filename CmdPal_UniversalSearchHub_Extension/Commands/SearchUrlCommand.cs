// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace CmdPal_UniversalSearchHub_Extension.Commands;

internal sealed partial class SearchUrlCommand : InvokableCommand
{
    private readonly string _url;

    internal SearchUrlCommand(string url) => _url = url;

    public override IconInfo Icon => new("\uE721");

    public override CommandResult Invoke()
    {
        Process.Start(new ProcessStartInfo(_url) { UseShellExecute = true });
        return CommandResult.Hide();
    }
}
