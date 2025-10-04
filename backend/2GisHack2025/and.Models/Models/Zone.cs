using System.Text.Json.Serialization;

namespace AntAlgorithm;

public class Zone
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("region")]
    public IEnumerable<Point> Region { get; set; }
    
    [JsonPropertyName("zone_type")]
    public ZoneType ZoneType { get; set; }
}