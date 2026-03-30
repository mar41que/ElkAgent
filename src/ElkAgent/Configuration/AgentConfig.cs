using System.Collections.Generic;

namespace ElkAgent.Configuration;

public class AgentConfig
{
    public ElasticsearchConfig Elasticsearch { get; set; } = new();
    public LogstashConfig Logstash { get; set; } = new();
    public CollectorConfig Collectors { get; set; } = new();
    public string OutputMode { get; set; } = "elasticsearch";
}

public class ElasticsearchConfig
{
    public string Url { get; set; } = "http://localhost:9200";
    public string IndexPrefix { get; set; } = "elkagent";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int BulkSize { get; set; } = 100;
    public int FlushIntervalMs { get; set; } = 5000;
}

public class LogstashConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5044;
    public string Protocol { get; set; } = "http"; // "http" | "tcp"
}

public class CollectorConfig
{
    public WindowsEventConfig WindowsEvent { get; set; } = new();
    public FileCollectorConfig File { get; set; } = new();
    public SyslogCollectorConfig Syslog { get; set; } = new();
}

public class WindowsEventConfig
{
    public bool Enabled { get; set; } = true;
    public List<string> Channels { get; set; } = new() { "System", "Application", "Security" };
    public int PollIntervalMs { get; set; } = 5000;
}

public class FileCollectorConfig
{
    public bool Enabled { get; set; } = false;
    public List<string> Paths { get; set; } = new();
    public int PollIntervalMs { get; set; } = 2000;
}

public class SyslogCollectorConfig
{
    public bool Enabled { get; set; } = false;
    public int Port { get; set; } = 514;
}