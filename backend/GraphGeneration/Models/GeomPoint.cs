using QuickGraph;
using VoronatorSharp;

namespace GraphGeneration.Models;

public class GeomPoint
{
    public int Id { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Weight { get; set; }
    public int Influence { get; set; }
    public bool Show { get; set; }

    public Vector2 AsVector2() => new Vector2((float)X, (float)Y);
    public bool IsPoi => Weight > 0;
    public Dictionary<int, bool> Paths { get; set; } = new();

    public void AddPath(GeomPoint p1, GeomPoint p2)
    {
        var minp = p1.Id < p2.Id ? p1.Id : p2.Id;
        var maxp = minp == p1.Id ? p2.Id : p1.Id;

        var combined = minp * 10000 + maxp;
        Paths.TryAdd(combined, true);
    }
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
        return dist;
    }

    public void SetWeight()
    {
        Weight = From.Influence + To.Influence;
    }
}