// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CmdPal_UniversalSearchHub_Extension.Models;

internal sealed class ProvidersDocument
{
    public int Version { get; set; } = 2;

    public List<SearchProvider> Providers { get; set; } = [];
}
