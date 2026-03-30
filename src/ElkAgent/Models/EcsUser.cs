using System.Text.Json.Serialization;

namespace ElkAgent.Models;

public class EcsUser
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }
}