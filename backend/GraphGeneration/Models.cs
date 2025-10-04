

using VoronatorSharp;

namespace GraphGeneration;


public class GraphEnrichmentConfig
{
    public double GridStep { get; set; } = 15.0; // метров
    public bool UseDelaunayForAvailable { get; set; } = true;
    public int MaxPointsPerZone { get; set; } = 500;
    public double ImpatienceFactor { get; set; } = 0.8; // коэффициент нетерпеливости
}