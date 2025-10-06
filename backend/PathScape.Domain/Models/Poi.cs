using System.Text.Json.Serialization;

namespace PathScape.Domain.Models;

public class Poi
{
    public Poi() {}

    public Poi(int id, double x, double y, double weight)
    {
        Id = id;
        Point = new Point() { X = x, Y = y };
        Weight = weight;
    }
    
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("point")]
    public required Point Point { get; set; }
    
    [JsonPropertyName("weight")]
    public double Weight { get; set; }
}