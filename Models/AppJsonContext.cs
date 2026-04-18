using System.Collections.Generic;
using System.Text.Json.Serialization;
using RonCafeApp.Models;

namespace RonCafeApp.Models;

[JsonSerializable(typeof(LauncherConfig))]
[JsonSerializable(typeof(List<AppItem>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class AppJsonContext : JsonSerializerContext { }