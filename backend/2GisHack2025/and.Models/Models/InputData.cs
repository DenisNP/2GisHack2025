using System.Text.Json.Serialization;

namespace AntAlgorithm;

public class InputData
{
    [JsonPropertyName("zones")]
    public Zone[] Zones { get; set; }
    
    [JsonPropertyName("pois")]
    public Poi[]  Pois { get; set; }
}