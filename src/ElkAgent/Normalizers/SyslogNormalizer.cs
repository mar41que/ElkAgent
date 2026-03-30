using ElkAgent.Models;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ElkAgent.Normalizers;

/// <summary>
/// Нормализует Syslog RFC 3164 / RFC 5424 → ECS
/// </summary>
public class SyslogNormalizer : INormalizer
{
    // RFC 3164: <PRI>TIMESTAMP HOSTNAME TAG: MESSAGE
    private static readonly Regex Rfc3164 = new(
        @"^<(?<pri>\d{1,3})>(?<timestamp>\w{3}\s+\d{1,2}\s\d{2}:\d{2}:\d{2})\s(?<hostname>\S+)\s(?<tag>[^:]+):\s*(?<message>.*)$",
        RegexOptions.Compiled);

    // RFC 5424: <PRI>VERSION TIMESTAMP HOSTNAME APP-NAME PROCID MSGID STRUCTURED-DATA MSG
    private static readonly Regex Rfc5424 = new(
        @"^<(?<pri>\d{1,3})>(?<version>\d)\s(?<timestamp>\S+)\s(?<hostname>\S+)\s(?<appname>\S+)\s(?<procid>\S+)\s(?<msgid>\S+)\s(?<structured>.*?)\s(?<message>.*)$",
        RegexOptions.Compiled);

    public bool CanHandle(RawEvent rawEvent) => rawEvent.Source == "syslog";

    public EcsEvent Normalize(RawEvent rawEvent)
    {
        var ecsEvent = new EcsEvent
        {
            Timestamp = rawEvent.Timestamp,
            Message = rawEvent.RawMessage,
            Event = new EcsEventDetails
            {
                Kind = "event",
                Original = rawEvent.RawMessage
            },
            Log = new EcsLog { Level = "info" },
            Tags = new List<string> { "elkagent", "syslog" }
        };

        if (rawEvent.Fields.TryGetValue("source_ip", out var ip))
            ecsEvent.Network = new EcsNetwork { Protocol = "udp" };

        var match3164 = Rfc3164.Match(rawEvent.RawMessage);
        if (match3164.Success)
        {
            ParsePriority(match3164.Groups["pri"].Value, ecsEvent);
            ecsEvent.Event.Provider = match3164.Groups["tag"].Value.Trim();
            ecsEvent.Message = match3164.Groups["message"].Value.Trim();
            return ecsEvent;
        }

        var match5424 = Rfc5424.Match(rawEvent.RawMessage);
        if (match5424.Success)
        {
            ParsePriority(match5424.Groups["pri"].Value, ecsEvent);
            ecsEvent.Event.Provider = match5424.Groups["appname"].Value;
            ecsEvent.Message = match5424.Groups["message"].Value.Trim();
            ecsEvent.Process = new EcsProcess
            {
                Name = match5424.Groups["appname"].Value
            };
            return ecsEvent;
        }

        return ecsEvent;
    }

    private static void ParsePriority(string priStr, EcsEvent ecsEvent)
    {
        if (!int.TryParse(priStr, out var pri)) return;

        var facility = pri >> 3;
        var severity = pri & 0x07;

        ecsEvent.Log.Level = severity switch
        {
            0 => "critical",
            1 => "critical",
            2 => "critical",
            3 => "error",
            4 => "warning",
            5 => "info",
            6 => "info",
            7 => "debug",
            _ => "info"
        };

        ecsEvent.Event.Severity = severity;
        ecsEvent.Labels["syslog_facility"] = facility.ToString();
        ecsEvent.Labels["syslog_severity"] = severity.ToString();
    }
}