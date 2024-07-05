using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unicorn.LiveLogViewer.Models;

[JsonSerializable(typeof(IEnumerable<LogEvent>))]
internal partial class LogViewerSerializerContext : JsonSerializerContext;