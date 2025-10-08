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
    public double Weight { get; set; }
    public double Influence { get; set; }
    public bool Show { get; set; }

    public Vector2 AsVector2() => new(Id, (float)X, (float)Y, Weight);
    public bool IsPoi => Weight > 0;
    
    public override string ToString() => $"({X}, {Y})";
}