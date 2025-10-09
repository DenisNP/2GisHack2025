using System.Collections.Generic;
using System.Text.Json.Serialization;
using PathScape.Domain.Models;

namespace WebApplication2.Dto;

public class Zone
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("region")]
    public required IEnumerable<Point> Region { get; init; }
    
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ZoneType ZoneType { get; init; }
}