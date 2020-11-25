using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

class Joke
{
    [JsonPropertyName("categories")]
    public string[] Categories { get; set; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; }
    
    [JsonPropertyName("icon_url")]
    public string IconUrl { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }
}