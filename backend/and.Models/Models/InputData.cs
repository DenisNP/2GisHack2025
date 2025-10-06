using System.Text.Json.Serialization;

namespace AntAlgorithm;

public class InputData
{
    [JsonPropertyName("zones")]
    public required Zone[] Zones { get; set; }
    
    [JsonPropertyName("poi")]
    public required Poi[]  Pois { get; set; }
}