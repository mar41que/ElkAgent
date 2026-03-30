using ElkAgent.Configuration;
using ElkAgent.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ElkAgent.Senders;

/// <summary>
/// Отправка событий в Logstash через HTTP
/// </summary>
public class LogstashSender : ISender
{
    private readonly LogstashConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<LogstashSender> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public LogstashSender(
        IOptions<AgentConfig> config,
        IHttpClientFactory httpClientFactory,
        ILogger<LogstashSender> logger)
    {
        _config = config.Value.Logstash;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("logstash");
        _httpClient.BaseAddress = new Uri($"http://{_config.Host}:{_config.Port}");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task SendBatchAsync(
        IReadOnlyList<EcsEvent> events,
        CancellationToken cancellationToken)
    {
        foreach (var evt in events)
        {
            try
            {
                var json = JsonSerializer.Serialize(evt, JsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/", content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                    _logger.LogWarning("Logstash returned {StatusCode}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send event to Logstash");
            }
        }
    }
}