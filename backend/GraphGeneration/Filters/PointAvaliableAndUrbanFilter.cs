using GraphGeneration.Models;
using NetTopologySuite.Geometries;
using VoronatorSharp;

namespace GraphGeneration.Filters;

public class PointAvaliableAndUrbanFilter :  IPointFilter
{
    private readonly PolygonMap _polygonMap;

    public PointAvaliableAndUrbanFilter(PolygonMap polygonMap)
    {
        _polygonMap = polygonMap;
    }

    private bool Skip(float x, float y)
    {
        var lineString = new Point(x, y);
            
        if (_polygonMap.Urban.Any(p => p.Contains(lineString)))
        {
            return !_polygonMap.Available.Any(p => p.Contains(lineString));
        }

        return true;
    }
    
    public bool Skip(Vector2 vector) => Skip(vector.x, vector.y);
}