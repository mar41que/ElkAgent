using ElkAgent;
using ElkAgent.Collectors;
using ElkAgent.Configuration;
using ElkAgent.Normalizers;
using ElkAgent.Pipeline;
using ElkAgent.Senders;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/agent-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "ElkAgent";
    });

    builder.Services.Configure<AgentConfig>(
        builder.Configuration.GetSection("Agent"));

    // коллекторы
    builder.Services.AddSingleton<ICollector, WindowsEventLogCollector>();
    builder.Services.AddSingleton<ICollector, FileLogCollector>();
    builder.Services.AddSingleton<ICollector, SyslogCollector>();

    // нормалзаторы
    builder.Services.AddSingleton<WindowsEventNormalizer>();
    builder.Services.AddSingleton<SyslogNormalizer>();
    builder.Services.AddSingleton<EcsNormalizer>();

    // сендеры
    builder.Services.AddHttpClient();
    builder.Services.AddSingleton<ElasticsearchSender>();
    builder.Services.AddSingleton<LogstashSender>();

    // пайпа
    builder.Services.AddSingleton<EventPipeline>();
    builder.Services.AddHostedService<Worker>();

    builder.Logging.AddSerilog();

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Agent terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}