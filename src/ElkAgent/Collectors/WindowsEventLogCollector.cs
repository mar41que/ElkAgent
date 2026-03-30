using ElkAgent.Configuration;
using ElkAgent.Models;
using Microsoft.Extensions.Options;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace ElkAgent.Collectors;

[SupportedOSPlatform("windows")]
public class WindowsEventLogCollector : ICollector
{
    private readonly WindowsEventConfig _config;
    private readonly ILogger<WindowsEventLogCollector> _logger;

    private readonly Dictionary<string, long> _lastRecordIds = new();

    public string Name => "windows_event";
    public bool IsEnabled => _config.Enabled;

    public WindowsEventLogCollector(
        IOptions<AgentConfig> config,
        ILogger<WindowsEventLogCollector> logger)
    {
        _config = config.Value.Collectors.WindowsEvent;
        _logger = logger;
    }

    public async IAsyncEnumerable<RawEvent> CollectAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var channel in _config.Channels)
            {
                var lastId = _lastRecordIds.GetValueOrDefault(channel, 0);

                // Первый запрос — только события за последнюю минуту
                var queryString = lastId == 0
                    ? "*[System[TimeCreated[timediff(@SystemTime) <= 60000]]]"
                    : $"*[System[EventRecordID > {lastId}]]";

                EventLogQuery query;
                try
                {
                    query = new EventLogQuery(channel, PathType.LogName, queryString);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create query for channel {Channel}", channel);
                    continue;
                }

                EventLogReader reader;
                try
                {
                    reader = new EventLogReader(query);
                }
                catch (UnauthorizedAccessException)
                {
                    _logger.LogWarning(
                        "Access denied to channel [{Channel}]. Run as Administrator to collect Security logs",
                        channel);
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cannot read channel {Channel}", channel);
                    continue;
                }

                using (reader)
                {
                    while (true)
                    {
                        EventRecord? record;
                        try { record = reader.ReadEvent(); }
                        catch { break; }

                        if (record == null) break;

                        using (record)
                        {
                            if (record.RecordId.HasValue)
                                _lastRecordIds[channel] = record.RecordId.Value;

                            yield return new RawEvent
                            {
                                Source = "windows_event",
                                Timestamp = record.TimeCreated.HasValue
                                    ? new DateTimeOffset(record.TimeCreated.Value.ToUniversalTime(), TimeSpan.Zero)
                                    : DateTimeOffset.UtcNow,
                                RawMessage = record.FormatDescription() ?? record.ToXml(),
                                Fields = new Dictionary<string, object>
                                {
                                    ["channel"] = channel,
                                    ["event_id"] = record.Id,
                                    ["level"] = record.Level ?? 4,
                                    ["provider"] = record.ProviderName ?? string.Empty,
                                    ["process_id"] = (long)(record.ProcessId ?? 0),
                                    ["thread_id"] = (long)(record.ThreadId ?? 0),
                                    ["user_id"] = record.UserId?.Value ?? string.Empty,
                                    ["keywords"] = record.Keywords ?? 0L,
                                    ["xml"] = record.ToXml()
                                }
                            };
                        }
                    }
                }
            }

            await Task.Delay(_config.PollIntervalMs, cancellationToken);
        }
    }
}