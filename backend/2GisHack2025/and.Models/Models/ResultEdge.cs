using System.Text.Json.Serialization;

namespace AntAlgorithm;

public class ResultEdge
{
    // [JsonPropertyName("id")]
    // public int Id { get; set; }
    
    [JsonPropertyName("from")]
    public Point From { get; set; }
    
    [JsonPropertyName("to")]
    public Point To { get; set; }
    
    [JsonPropertyName("weight")]
    public double Weight { get; set; }
}

public class ResultPoint
{
    [JsonPropertyName("x")]
    public double X { get; set; }
    [JsonPropertyName("y")]
    public double Y { get; set; }
    [JsonPropertyName("weight")]
    public double Weight { get; set; }
}