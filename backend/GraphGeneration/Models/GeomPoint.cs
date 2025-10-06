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

public struct GeomPoint : IComparable
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
    
    public bool Equals(GeomPoint other) => X == other.X && Y == other.Y;

    public override bool Equals(object? obj)
    {
        if (obj is GeomPoint p)
        {
            return Equals(p);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return (X, Y).GetHashCode();
    }
    
    public int CompareTo(object? obj)
    {
        if (obj is GeomPoint point)
        {
            int xComparison = X.CompareTo(point.X);
            if (xComparison != 0)
                return xComparison;
            
            return Y.CompareTo(point.Y);
        }
        
        throw new InvalidCastException(obj?.GetType().Name);
    }

    public int Id { get; }
    public float X { get; }
    public float Y { get; }
    public double Weight { get; }
    public int Influence { get; set; }
    public bool Show { get; set; }

    public Vector2 AsVector2() => new(Id, (float)X, (float)Y, Weight);
    public bool IsPoi => Weight > 0;
    public Dictionary<int, bool> Paths { get; } = new();

    public void AddPath(GeomPoint p1, GeomPoint p2)
    {
        var minp = p1.Id < p2.Id ? p1.Id : p2.Id;
        var maxp = minp == p1.Id ? p2.Id : p1.Id;

        var combined = minp * 10000 + maxp;
        Paths.TryAdd(combined, true);
    }
    
    public override string ToString() => $"({X}, {Y})";
}

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