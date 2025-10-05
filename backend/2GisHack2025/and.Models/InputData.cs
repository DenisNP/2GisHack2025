using System.Text.Json.Serialization;

namespace and.Models;

public class InputData
{
    [JsonPropertyName("zones")]
    public Zone[] Zones { get; set; }
    
    [JsonPropertyName("poi")]
    public Poi[]  Pois { get; set; }
}