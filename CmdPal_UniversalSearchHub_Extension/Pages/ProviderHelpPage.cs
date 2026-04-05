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
            $$$"""
            # CmdPal Search Hub

            **Two places to configure things:**

            - **Command Palette Settings** (Windows Settings app) → **Extensions** → **CmdPal Search Hub** → **Extension settings** — query suggestions, in-palette preview, and preview API keys (same fields as below; stored in `providers.json`).
            - **Configure search providers** (extension command in CmdPal) — full **engine list** (built-in and custom URLs), enable/disable, abbreviations, and **Preview API keys** form.

            **Data file:** `{{{path}}}`

            Each provider has a **URL template** that must include **`{0}`** once. The search text is URL-encoded and substituted for `{0}`.

            **Built-in** providers can be disabled but not removed. **Custom** providers can be edited or removed.

            Use **abbreviation + space + query** in the main hub (for example `g weather`) when a provider is enabled.

            ## Suggestions and preview

            **Query suggestions** (Google and YouTube only) are **on by default** for new installs; turn them off in **Extension settings** or **Configure search providers** if you want no network suggest calls. Suggestions use public suggest endpoints (no API key for suggestions).

            **In-palette result preview** adds a **More** action (and **Ctrl+Enter** when the host shows it) on hub rows. YouTube preview needs a **YouTube Data API v3** key; Google web preview needs a **Programmable Search** JSON API key and **search engine ID (cx)**. Set them in **Extension settings** or under **Preview API keys** in **Configure search providers**.

            Other built-in providers open a normal search URL in preview when preview is enabled.
            """;
        return [new MarkdownContent(body)];
    }
}
