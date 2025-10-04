

using VoronatorSharp;

namespace GraphGeneration;


// 
public record Point(double X, double Y);

public enum ZoneType
{
    Restricted,    // непроходимая область
    Urban,         // тротуары
    Available      // газоны
}

public class Zone
{
    public int Id { get; set; }
    public List<Point> Region { get; set; } = new();
    public ZoneType Type { get; set; }
}


public class GraphEnrichmentConfig
{
    public double GridStep { get; set; } = 15.0; // метров
    public bool UseDelaunayForAvailable { get; set; } = true;
    public int MaxPointsPerZone { get; set; } = 500;
    public double ImpatienceFactor { get; set; } = 0.8; // коэффициент нетерпеливости
}