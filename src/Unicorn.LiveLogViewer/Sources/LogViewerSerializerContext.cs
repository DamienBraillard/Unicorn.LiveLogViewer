using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unicorn.LiveLogViewer.Sources;

[JsonSerializable(typeof(IEnumerable<LogEvent>))]
internal partial class LogViewerSerializerContext : JsonSerializerContext;