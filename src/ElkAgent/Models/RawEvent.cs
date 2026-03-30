using System;
using System.Collections.Generic;

namespace ElkAgent.Models;

public class RawEvent
{
    public string Source { get; set; } = string.Empty; // windows_event | file | syslog
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string RawMessage { get; set; } = string.Empty;
    public Dictionary<string, object> Fields { get; set; } = new();
}