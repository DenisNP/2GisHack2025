using GraphGeneration.Models;
using NetTopologySuite.Geometries;
using VoronatorSharp;

namespace GraphGeneration.Filters;

public class EdgeInRestrictedFilter : IEdgeFilter 
{
    private readonly PolygonMap _polygonMap;
    private IPointFilter _pointFilter;
    private readonly IEdgeFilter _longFilter;

    public EdgeInRestrictedFilter(PolygonMap polygonMap, float hexSize)
    {
        _polygonMap = polygonMap;
        _pointFilter = new PointRestrictedAndNotUrbanFilter(polygonMap);
        _longFilter = new EdgeFakeFilter(polygonMap, hexSize);
    }
    
    public bool Skip(Vector2 a, Vector2 b)
    {
        if (_pointFilter.Skip(a) && _pointFilter.Skip(b))
        {
            return true;
        }

        // if (_longFilter.Skip(a, b))
        // {
        //     return true;
        // }

        // if (a.IsPoi && b.IsPoi)
        // {
        //     return false;
        // }
        
        // Создаем геометрическое представление ребра
        var lineString = new LineString([
            new Coordinate(a.x, a.y),
            new Coordinate(b.x, b.y)
        ]);

        if (_polygonMap.Restricted.Any(p => p.Crosses(lineString) || p.Contains(lineString)))
        {
            var crossUrban = _polygonMap.Urban.Any(p => p.Crosses(lineString) || p.Contains(lineString));
            // var anyPointInRestrict = _polygonMap.Restricted.Any(p => p.Contains(new Point(a.x, a.y)) || p.Contains(new Point(b.x, b.y)));
            var anyPointInRestrict = _pointFilter.Skip(a) || _pointFilter.Skip(b);
            return !crossUrban || anyPointInRestrict;
        }

        return false;
    }
}