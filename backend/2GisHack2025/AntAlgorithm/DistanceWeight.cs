using and.Models;

namespace AntAlgorithm;

internal class DistanceWeight
{
    public double Distance { get; set; }
    public double Weight { get; set; } = 0;
    
    public Poi From { get; set; }
    public Poi To { get; set; }
}