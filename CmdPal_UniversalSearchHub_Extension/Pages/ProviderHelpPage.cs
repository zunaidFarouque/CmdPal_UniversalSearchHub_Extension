// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CmdPal_UniversalSearchHub_Extension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace CmdPal_UniversalSearchHub_Extension.Pages;

internal sealed partial class ProviderHelpPage : ContentPage
{
    public ProviderHelpPage()
    {
        Icon = new IconInfo("\uE946");
        Title = "Search providers help";
        Name = "Open";
    }

    public override IContent[] GetContent()
    {
        string path = ProviderService.Instance.ProvidersFilePath;
        string body =
            $"""
            # Universal Search Hub

            **Data file:** `{path}`

            Each provider has a **URL template** that must include **`{{0}}`** once. The search text is URL-encoded and substituted for `{{0}}`.

            **Built-in** providers can be disabled but not removed. **Custom** providers can be edited or removed.

            Use **prefix + space + query** in the main hub (for example `g weather`) when a provider is enabled.
            """;
        return [new MarkdownContent(body)];
    }
}
