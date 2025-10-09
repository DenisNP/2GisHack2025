using System.Text.Json.Serialization;
using PathScape.Domain.Models;

namespace AntAlgorithm;

[JsonSerializable(typeof(Path))]
public class Path
{
    [JsonPropertyName("start")]
    public Poi Start { get; set; }
    
    [JsonPropertyName("end")]
    public Poi End { get; set; }
    
    [JsonPropertyName("points")]
    public IEnumerable<Poi> Points { get; set; }
}