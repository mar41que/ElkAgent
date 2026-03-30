using ElkAgent.Models;
using System.Collections.Generic;

namespace ElkAgent.Normalizers;

/// <summary>
/// Нормализует Windows Event Log события в ECS формат.
/// Маппинг уровней: https://www.elastic.co/guide/en/ecs/current/ecs-event.html
/// </summary>
public class WindowsEventNormalizer : INormalizer
{
    public bool CanHandle(RawEvent rawEvent) => rawEvent.Source == "windows_event";

    public EcsEvent Normalize(RawEvent rawEvent)
    {
        var fields = rawEvent.Fields;
        var eventId = GetInt(fields, "event_id");
        var level = GetInt(fields, "level");
        var provider = GetString(fields, "provider");
        var channel = GetString(fields, "channel");

        var ecsEvent = new EcsEvent
        {
            Timestamp = rawEvent.Timestamp,
            Message = rawEvent.RawMessage,
            Event = new EcsEventDetails
            {
                Kind = "event",
                Provider = provider,
                Code = eventId.ToString(),
                Severity = level,
                Original = GetString(fields, "xml"),
                Category = MapCategory(channel, eventId),
                Type = MapType(level, eventId),
                Outcome = MapOutcome(eventId),
                Action = MapAction(eventId)
            },
            Log = new EcsLog
            {
                Level = MapLogLevel(level),
                Logger = channel
            },
            Process = new EcsProcess
            {
                Pid = GetLong(fields, "process_id")
            },
            User = new EcsUser
            {
                Id = GetString(fields, "user_id")
            },
            Tags = new List<string> { "elkagent", "windows", channel.ToLower() }
        };

        ecsEvent.Labels["channel"] = channel;
        ecsEvent.Labels["event_id"] = eventId.ToString();

        return ecsEvent;
    }

    private static List<string> MapCategory(string channel, int eventId) =>
        channel.ToLower() switch
        {
            "security" => new List<string> { "authentication", "iam" },
            "system" => new List<string> { "host", "process" },
            "application" => new List<string> { "configuration" },
            _ => new List<string> { "host" }
        };

    private static List<string> MapType(int level, int eventId)
    {
        if (eventId == 4624 || eventId == 4625)
            return new List<string> { "info", "access" };

        return level switch
        {
            1 => new List<string> { "error" },
            2 => new List<string> { "error" },
            3 => new List<string> { "info" },
            _ => new List<string> { "info" }
        };
    }

    private static string? MapOutcome(int eventId) => eventId switch
    {
        4624 => "success",
        4625 => "failure",
        4648 => "success",
        _ => null
    };

    private static string? MapAction(int eventId) => eventId switch
    {
        4624 => "logon",
        4625 => "logon-failed",
        4634 => "logoff",
        4648 => "explicit-logon",
        4720 => "user-account-created",
        4726 => "user-account-deleted",
        4732 => "member-added-to-group",
        7036 => "service-state-changed",
        7045 => "service-installed",
        _ => null
    };

    private static string MapLogLevel(int level) => level switch
    {
        1 => "critical",
        2 => "error",
        3 => "warning",
        4 => "info",
        5 => "debug",
        _ => "info"
    };

    private static int GetInt(Dictionary<string, object> f, string key) =>
        f.TryGetValue(key, out var v) && v is long l ? (int)l :
        f.TryGetValue(key, out var v2) && v2 is int i ? i : 0;

    private static long GetLong(Dictionary<string, object> f, string key) =>
        f.TryGetValue(key, out var v) && v is long l ? l : 0;

    private static string GetString(Dictionary<string, object> f, string key) =>
        f.TryGetValue(key, out var v) ? v?.ToString() ?? string.Empty : string.Empty;
}