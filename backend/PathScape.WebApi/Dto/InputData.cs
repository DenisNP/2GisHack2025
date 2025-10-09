using System.Text.Json.Serialization;
using PathScape.Domain.Models;

namespace WebApplication2.Dto;

public class InputData
{
    [JsonPropertyName("zones")]
    public required Zone[] Zones { get; set; }
    
    [JsonPropertyName("poi")]
    public required Poi[]  Pois { get; set; }
}