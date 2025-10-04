using System.Text.Json.Serialization;

namespace AntAlgorithm;

public class Poi
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("point")]
    public Point Point { get; set; }
    [JsonPropertyName("weight")]
    public double Weight { get; set; }
}