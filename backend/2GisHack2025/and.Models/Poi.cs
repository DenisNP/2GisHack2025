using System.Text.Json.Serialization;

namespace and.Models;

public class Poi
{
    public Poi() {}

    public Poi(int id, double x, double y, double Weight)
    {
        Id = id;
        Point = new Point() { X = x, Y = y };
        this.Weight = Weight;
    }
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("point")]
    public Point Point { get; set; }
    [JsonPropertyName("weight")]
    public double Weight { get; set; }
}