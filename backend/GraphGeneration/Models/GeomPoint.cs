using VoronatorSharp;

namespace GraphGeneration.Models;

public interface IEdge<TVertex>
{
    /// <summary>Gets the source vertex</summary>
    /// <getter>
    ///   <ensures>Contract.Result&lt;TVertex&gt;() != null</ensures>
    /// </getter>
    TVertex Source { get; }

    /// <summary>Gets the target vertex</summary>
    /// <getter>
    ///   <ensures>Contract.Result&lt;TVertex&gt;() != null</ensures>
    /// </getter>
    TVertex Target { get; }
}

public class GeomPoint
{
    public GeomPoint(int id, float x, float y, double weight)
    {
        Id = id;
        X = x;
        Y = y;
        Weight = weight;
    }
    
    public GeomPoint(Vector2 vector2)
    {
        Id = vector2.Id;
        X = vector2.x;
        Y = vector2.y;
        Weight = vector2.Weight;
    }

    public override bool Equals(object? obj)
    {
        if (obj is GeomPoint p)
        {
            return Id == p.Id;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return (X, Y).GetHashCode();
    }

    public int Id { get; set; }
    public double X { get; }
    public double Y { get; }
    public double Weight { get; set; }
    public int Influence { get; set; }
    public bool Show { get; set; }

    public Vector2 AsVector2() => new Vector2(Id, (float)X, (float)Y, Weight);
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
    public GeomEdge()
    {
    }
    
    public GeomEdge(GeomPoint from, GeomPoint to, double weight)
    {
        From = from;
        To = to;
        Weight = weight;
    }

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