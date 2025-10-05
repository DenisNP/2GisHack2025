using QuickGraph;
using VoronatorSharp;

namespace GraphGeneration.Models;

public class GeomPoint
{
    public int Id { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Weight { get; set; }
    public double Influence { get; set; }

    public Vector2 AsVector2() => new Vector2((float)X, (float)Y);
    public bool IsPoi => Weight > 0;
}

public class GeomEdge : IEdge<GeomPoint>
{
    public GeomPoint From { get; set; }
    public GeomPoint To { get; set; }
    public double Weight { get; set; }

    public int Id => From.Id < To.Id ? From.Id * 10000 + To.Id : To.Id * 10000 + From.Id;

    public GeomPoint Source => From;
    public GeomPoint Target => To;

    public void IncreaseWeight()
    {
        Weight += From.Influence + To.Influence;
    }

    public double Cost()
    {
        var dist = Vector2.Distance(Source.AsVector2(), Target.AsVector2());
        return Math.Max(0.1, dist * (1 - Weight / 5));
        return dist;
    }
}