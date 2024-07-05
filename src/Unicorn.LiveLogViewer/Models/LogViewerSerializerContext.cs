using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unicorn.LiveLogViewer.Models;

[JsonSerializable(typeof(IEnumerable<LogEvent>))]
[JsonSerializable(typeof(IEnumerable<LogSourceInfo>))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class LogViewerSerializerContext : JsonSerializerContext;