using System.Text.Json.Serialization;
using AntAlgorithm;

[JsonSerializable(typeof(Result))]
public class Result
{
    [JsonPropertyName("from")]
    public Poi From { get; set; }
    
    [JsonPropertyName("to")]
    public Poi To { get; set; }
    
    [JsonPropertyName("weight")]
    public double Weight { get; set; }
}