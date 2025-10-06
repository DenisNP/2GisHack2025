using VoronatorSharp;

namespace GraphGeneration.Models;

public class GeomPoint : IComparable
{
    public GeomPoint(int id, float x, float y, double weight)
    {
        Id = id;
        X = x;
        Y = y;
        Weight = weight;
    }

    private bool Equals(GeomPoint other) => X == other.X && Y == other.Y;

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