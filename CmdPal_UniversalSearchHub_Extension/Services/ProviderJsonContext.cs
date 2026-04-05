// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using CmdPal_UniversalSearchHub_Extension.Models;

namespace CmdPal_UniversalSearchHub_Extension.Services;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(SearchProvider))]
[JsonSerializable(typeof(List<SearchProvider>))]
[JsonSerializable(typeof(SearchProvider[]))]
[JsonSerializable(typeof(HubSettings))]
[JsonSerializable(typeof(ProvidersDocument))]
internal sealed partial class ProviderJsonContext : JsonSerializerContext
{
}
