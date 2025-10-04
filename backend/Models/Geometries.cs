namespace Models;

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
    public List<Point> Region { get; set; } = [];
    public ZoneType Type { get; set; }
}

public class Poi
{
    public int Id { get; set; }
    public Point Point { get; set; } = new(0, 0);
    public double Weight { get; set; }
}

public class Path
{
    public Poi Start { get; set; } = new();
    public Poi End { get; set; } = new();
    public List<Point> Points { get; set; } = [];
}

public class GraphEnrichmentConfig
{
    public double GridStep { get; set; } = 15.0; // метров
    public bool UseDelaunayForAvailable { get; set; } = true;
    public int MaxPointsPerZone { get; set; } = 500;
    public double ImpatienceFactor { get; set; } = 0.8; // коэффициент нетерпеливости
}         