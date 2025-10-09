using NetTopologySuite.Geometries;
using VoronatorSharp;

namespace GraphGeneration.Filters;

public class PointAllowedFilter:  IPointFilter
{
    private readonly IReadOnlyCollection<Polygon> _allowed;

    public PointAllowedFilter(IReadOnlyCollection<Polygon> allowed)
    {
        _allowed = allowed;
    }

    private bool Skip(float x, float y)
    {
        var point = new Point(x, y);
            
        foreach (var polygon in _allowed)
        {
            if (polygon.Contains(point))
            {
                return false;
            }
        }

        return true;
    }
    
    public bool Skip(Vector2 vector) => Skip(vector.X, vector.Y);
}