using GraphGeneration.Models;
using NetTopologySuite.Geometries;
using VoronatorSharp;

namespace GraphGeneration.Filters;

public class PointRestrictedUrbanOrAvailableFilter : IPointFilter
{
    private readonly PolygonMap _polygonMap;

    public PointRestrictedUrbanOrAvailableFilter(PolygonMap polygonMap)
    {
        _polygonMap = polygonMap;
    }
    
    public bool Skip(Vector2 vector)
    {
        var point = new Point(vector.X, vector.Y);
        return _polygonMap.Restricted.Any(p => p.Contains(point))
               || _polygonMap.Available.Any(a => a.Contains(point))
               || _polygonMap.Urban.Any(u => u.Contains(point));
    }
}