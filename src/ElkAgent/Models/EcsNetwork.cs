using System.Text.Json.Serialization;

namespace ElkAgent.Models;

public class EcsNetwork
{
    [JsonPropertyName("protocol")]
    public string? Protocol { get; set; }

    [JsonPropertyName("direction")]
    public string? Direction { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}