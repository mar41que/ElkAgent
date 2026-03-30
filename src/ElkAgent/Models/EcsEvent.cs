using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ElkAgent.Models;

/// <summary>
/// Elastic Common Schema (ECS) v8 совместимая модель события
/// https://www.elastic.co/guide/en/ecs/current/ecs-field-reference.html
/// </summary>
public class EcsEvent
{
    [JsonPropertyName("@timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("event")]
    public EcsEventDetails Event { get; set; } = new();

    [JsonPropertyName("host")]
    public EcsHost Host { get; set; } = new();

    [JsonPropertyName("process")]
    public EcsProcess? Process { get; set; }

    [JsonPropertyName("user")]
    public EcsUser? User { get; set; }

    [JsonPropertyName("network")]
    public EcsNetwork? Network { get; set; }

    [JsonPropertyName("log")]
    public EcsLog Log { get; set; } = new();

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("labels")]
    public Dictionary<string, string> Labels { get; set; } = new();

    // ECS version field
    [JsonPropertyName("ecs")]
    public EcsVersion Ecs { get; set; } = new() { Version = "8.0.0" };
}

public class EcsEventDetails
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "event"; // event | alert | metric | state

    [JsonPropertyName("category")]
    public List<string> Category { get; set; } = new(); // authentication | network | process | file

    [JsonPropertyName("type")]
    public List<string> Type { get; set; } = new(); // start | end | info | error | access | change

    [JsonPropertyName("outcome")]
    public string? Outcome { get; set; } // success | failure | unknown

    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("severity")]
    public int? Severity { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("original")]
    public string? Original { get; set; }
}

public class EcsLog
{
    [JsonPropertyName("level")]
    public string Level { get; set; } = "info";

    [JsonPropertyName("logger")]
    public string? Logger { get; set; }

    [JsonPropertyName("origin")]
    public EcsLogOrigin? Origin { get; set; }
}

public class EcsLogOrigin
{
    [JsonPropertyName("file")]
    public EcsLogFile? File { get; set; }
}

public class EcsLogFile
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("line")]
    public int? Line { get; set; }
}

public class EcsVersion
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "8.0.0";
}