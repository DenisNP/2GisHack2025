using NetTopologySuite.Geometries;
using VoronatorSharp;

namespace GraphGeneration.Filters;

public class PointIgnoreFilter
{
    private readonly IReadOnlyCollection<NetTopologySuite.Geometries.Polygon> _ignore;

    public PointIgnoreFilter(IReadOnlyCollection<NetTopologySuite.Geometries.Polygon> ignore)
    {
        _ignore = ignore;
    }

    private bool Skip(float x, float y)
    {
        var lineString = new Point(x, y);
            
        // Проверяем, пересекает ли ребро любой из игнорируемых полигонов
        foreach (var polygon in _ignore)
        {
            if (lineString.Crosses(polygon) || polygon.Contains(lineString))
            {
                return true;
            }
        }

        return false;
    }
    
    public bool Skip(Vector2 vector) => Skip(vector.x, vector.y);
}