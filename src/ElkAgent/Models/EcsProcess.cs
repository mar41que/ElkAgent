using System.Text.Json.Serialization;

namespace ElkAgent.Models;

public class EcsProcess
{
    [JsonPropertyName("pid")]
    public long? Pid { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("executable")]
    public string? Executable { get; set; }

    [JsonPropertyName("command_line")]
    public string? CommandLine { get; set; }

    [JsonPropertyName("parent")]
    public EcsProcessParent? Parent { get; set; }
}

public class EcsProcessParent
{
    [JsonPropertyName("pid")]
    public long? Pid { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}