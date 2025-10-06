using VoronatorSharp;

namespace GraphGeneration.Models;

public class GeomEdge : IEdge<GeomPoint>
{
    public GeomEdge(GeomPoint from, GeomPoint to, double weight)
    {
        From = from;
        To = to;
        Weight = weight;
    }

    public GeomPoint From { get; }
    public GeomPoint To { get; }
    public double Weight { get; private set; }

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