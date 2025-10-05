using GraphGeneration.Models;
using NetTopologySuite.Geometries;
using VoronatorSharp;

namespace GraphGeneration.Filters;

public class PointRestrictedAndNotUrbanFilter :  IPointFilter
{
    private readonly PolygonMap _polygonMap;

    public PointRestrictedAndNotUrbanFilter(PolygonMap polygonMap)
    {
        _polygonMap = polygonMap;
    }

    private bool Skip(float x, float y)
    {
        var lineString = new Point(x, y);
            
        // Restricted и не Urban - delete
        if (_polygonMap.Restricted.Any(p => p.Contains(lineString)))
        {
            return !_polygonMap.Urban.Any(p => p.Contains(lineString));
        }

        return false;
    }
    
    public bool Skip(Vector2 vector) => Skip(vector.x, vector.y);
}