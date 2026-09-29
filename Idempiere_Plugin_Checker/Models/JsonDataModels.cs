using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Models;

public class Root
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("s")]
    public List<int> S { get; set; } = new();

    [JsonPropertyName("data")]
    public List<Data> Data { get; set; } = new();
}

public class Data
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("fragment")]
    public bool Fragment { get; set; }

    [JsonPropertyName("stateRaw")]
    public int StateRaw { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("symbolicName")]
    public string SymbolicName { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
}