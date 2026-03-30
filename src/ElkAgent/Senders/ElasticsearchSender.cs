using ElkAgent.Configuration;
using ElkAgent.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ElkAgent.Senders;

/// <summary>
/// отправка событий в Elasticsearch через Bulk API
/// дока https://www.elastic.co/guide/en/elasticsearch/reference/current/docs-bulk.html
/// </summary>
public class ElasticsearchSender : ISender
{
    private readonly ElasticsearchConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ElasticsearchSender> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public ElasticsearchSender(
        IOptions<AgentConfig> config,
        IHttpClientFactory httpClientFactory,
        ILogger<ElasticsearchSender> logger)
    {
        _config = config.Value.Elasticsearch;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("elasticsearch");
        _httpClient.BaseAddress = new Uri(_config.Url);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        if (!string.IsNullOrEmpty(_config.Username))
        {
            var credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{_config.Username}:{_config.Password}"));
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);
        }
    }

    public async Task SendBatchAsync(
        IReadOnlyList<EcsEvent> events,
        CancellationToken cancellationToken)
    {
        if (events.Count == 0) return;

        var indexName = $"{_config.IndexPrefix}-{DateTime.UtcNow:yyyy.MM.dd}";
        var sb = new StringBuilder();

        foreach (var evt in events)
        {
            sb.AppendLine(JsonSerializer.Serialize(
                new { index = new { _index = indexName } }));
            sb.AppendLine(JsonSerializer.Serialize(evt, JsonOptions));
        }

        var content = new StringContent(sb.ToString(), Encoding.UTF8, "application/x-ndjson");

        try
        {
            var response = await _httpClient.PostAsync("/_bulk", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("ES Bulk API error {StatusCode}: {Body}",
                    response.StatusCode, body[..Math.Min(500, body.Length)]);
                return;
            }

            var result = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(result);
            var hasErrors = doc.RootElement.GetProperty("errors").GetBoolean();

            if (hasErrors)
                _logger.LogWarning("ES Bulk API reported errors for some documents");
            else
                _logger.LogDebug("Successfully indexed {Count} events to {Index}",
                    events.Count, indexName);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Cannot connect to Elasticsearch at {Url}", _config.Url);
        }
    }
}