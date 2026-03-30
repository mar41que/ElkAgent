using ElkAgent.Models;
using System.Collections.Generic;

namespace ElkAgent.Normalizers;

/// <summary>
/// Fallback-нормализатор — применяется для файловых логов и неизвестных источников
/// </summary>
public class EcsNormalizer : INormalizer
{
    private readonly List<INormalizer> _specializedNormalizers;

    public EcsNormalizer(
        WindowsEventNormalizer windowsNormalizer,
        SyslogNormalizer syslogNormalizer)
    {
        _specializedNormalizers = new List<INormalizer>
        {
            windowsNormalizer,
            syslogNormalizer
        };
    }

    public bool CanHandle(RawEvent rawEvent) => true; // fallback

    public EcsEvent Normalize(RawEvent rawEvent)
    {
        // попытка найти специализированный нормализатор
        var normalizer = _specializedNormalizers.FirstOrDefault(n => n.CanHandle(rawEvent));
        if (normalizer != null)
            return normalizer.Normalize(rawEvent);

        // Generic fallback
        return new EcsEvent
        {
            Timestamp = rawEvent.Timestamp,
            Message = rawEvent.RawMessage,
            Event = new EcsEventDetails
            {
                Kind = "event",
                Provider = rawEvent.Source,
                Original = rawEvent.RawMessage,
                Category = new List<string> { "host" },
                Type = new List<string> { "info" }
            },
            Log = new EcsLog
            {
                Level = "info",
                Logger = rawEvent.Source
            },
            Tags = new List<string> { "elkagent", rawEvent.Source }
        };
    }
}