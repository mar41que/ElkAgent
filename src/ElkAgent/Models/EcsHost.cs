using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ElkAgent.Models;

public class EcsHost
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = Environment.MachineName;

    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = Environment.MachineName;

    [JsonPropertyName("os")]
    public EcsOs Os { get; set; } = new();

    [JsonPropertyName("architecture")]
    public string Architecture { get; set; } = Environment.Is64BitOperatingSystem ? "x86_64" : "x86";

    [JsonPropertyName("ip")]
    public List<string> Ip { get; set; } = new();
}

public class EcsOs
{
    [JsonPropertyName("platform")]
    public string Platform { get; set; } = "windows";

    [JsonPropertyName("name")]
    public string Name { get; set; } = Environment.OSVersion.VersionString;

    [JsonPropertyName("version")]
    public string Version { get; set; } = Environment.OSVersion.Version.ToString();

    [JsonPropertyName("family")]
    public string Family { get; set; } = "windows";
}