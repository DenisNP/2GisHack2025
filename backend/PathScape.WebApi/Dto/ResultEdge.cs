using System.Text.Json.Serialization;
using PathScape.Domain.Models;

namespace WebApplication2.Dto;

public class ResultEdge
{
    // [JsonPropertyName("id")]
    // public int Id { get; set; }
    
    [JsonPropertyName("from")]
    public required Point From { get; set; }
    
    [JsonPropertyName("to")]
    public required Point To { get; set; }
    
    [JsonPropertyName("weight")]
    public double Weight { get; set; }
}

public class ResultPoint
{
    [JsonPropertyName("x")]
    public required double X { get; set; }
    [JsonPropertyName("y")]
    public required double Y { get; set; }
    [JsonPropertyName("weight")]
    public required double Weight { get; set; }
}