using ElkAgent.Configuration;
using ElkAgent.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ElkAgent.Collectors;

public class FileLogCollector : ICollector
{
    private readonly FileCollectorConfig _config;
    private readonly ILogger<FileLogCollector> _logger;
    private readonly Dictionary<string, long> _filePositions = new();

    public string Name => "file";
    public bool IsEnabled => _config.Enabled;

    public FileLogCollector(
        IOptions<AgentConfig> config,
        ILogger<FileLogCollector> logger)
    {
        _config = config.Value.Collectors.File;
        _logger = logger;
    }

    public async IAsyncEnumerable<RawEvent> CollectAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var path in _config.Paths)
            {
                if (!File.Exists(path)) continue;

                var position = _filePositions.GetValueOrDefault(path, 0);

                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                stream.Seek(position, SeekOrigin.Begin);
                using var reader = new StreamReader(stream);

                string? line;
                while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    yield return new RawEvent
                    {
                        Source = "file",
                        Timestamp = DateTimeOffset.UtcNow,
                        RawMessage = line,
                        Fields = new Dictionary<string, object>
                        {
                            ["file_path"] = path,
                            ["file_name"] = Path.GetFileName(path)
                        }
                    };
                }

                _filePositions[path] = stream.Position;
            }

            await Task.Delay(_config.PollIntervalMs, cancellationToken);
        }
    }
}