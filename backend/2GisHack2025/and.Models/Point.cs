
using System.Text.Json.Serialization;

namespace and.Models;

public class Point
{
    // [JsonPropertyName("id")]
    // public int Id { get; set; }
    
    [JsonPropertyName("x")]
    public double X { get; set; }
    
    [JsonPropertyName("y")]
    public double Y { get; set; }
}