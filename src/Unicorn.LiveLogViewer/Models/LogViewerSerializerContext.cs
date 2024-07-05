using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unicorn.LiveLogViewer.Models;

[JsonSerializable(typeof(IEnumerable<LogEvent>))]
[JsonSerializable(typeof(IEnumerable<LogSourceInfo>))]
internal partial class LogViewerSerializerContext : JsonSerializerContext;