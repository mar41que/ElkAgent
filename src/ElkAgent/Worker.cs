using ElkAgent.Pipeline;

namespace ElkAgent;

public class Worker : BackgroundService
{
    private readonly EventPipeline _pipeline;
    private readonly ILogger<Worker> _logger;

    public Worker(EventPipeline pipeline, ILogger<Worker> logger)
    {
        _pipeline = pipeline;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ElkAgent started at: {time}", DateTimeOffset.UtcNow);

        await _pipeline.StartAsync(stoppingToken);

        _logger.LogInformation("ElkAgent stopped at: {time}", DateTimeOffset.UtcNow);
    }
}