using NetTopologySuite.Geometries;
using VoronatorSharp;

namespace GraphGeneration.Filters;

public class PointAllowedFilter
{
    private readonly IReadOnlyCollection<NetTopologySuite.Geometries.Polygon> _allowed;

    public PointAllowedFilter(IReadOnlyCollection<NetTopologySuite.Geometries.Polygon> allowed)
    {
        _allowed = allowed;
    }

    public bool Skip(float x, float y)
    {
        var lineString = new Point(x, y);
            
        foreach (var polygon in _allowed)
        {
            if (lineString.Crosses(polygon) || polygon.Contains(lineString))
            {
                return false;
            }
        }

        return true;
    }
    
    public bool Skip(Vector2 vector) => Skip(vector.X, vector.Y);
}