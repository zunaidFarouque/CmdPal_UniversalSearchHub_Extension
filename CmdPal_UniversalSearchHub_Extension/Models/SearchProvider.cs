// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CmdPal_UniversalSearchHub_Extension.Models;

internal sealed class SearchProvider
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string Prefix { get; set; } = "";

    /// <summary>Must contain <c>{0}</c> for the URL-encoded search query.</summary>
    public string BaseUrl { get; set; } = "";

    public bool Enabled { get; set; } = true;

    public bool IsBuiltIn { get; set; }
}
