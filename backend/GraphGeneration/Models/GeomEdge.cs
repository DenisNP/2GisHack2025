using VoronatorSharp;

namespace GraphGeneration.Models;

public class GeomEdge : IEdge<GeomPoint>
{
    public GeomEdge(GeomPoint from, GeomPoint to)
    {
        From = from;
        To = to;
    }

    public GeomPoint From { get; }
    public GeomPoint To { get; }

    public int Id => From.Id < To.Id ? From.Id * 10000 + To.Id : To.Id * 10000 + From.Id;

    public GeomPoint Source => From;
    public GeomPoint Target => To;

    public double Cost()
    {
        var dist = Vector2.Distance(Source.AsVector2(), Target.AsVector2());
        return dist;
    }
}