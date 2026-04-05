// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CmdPal_UniversalSearchHub_Extension.Models;

internal sealed class ProvidersDocument
{
    public int Version { get; set; } = 3;

    public HubSettings Hub { get; set; } = HubSettings.CreateDefault();

    public List<SearchProvider> Providers { get; set; } = [];
}
