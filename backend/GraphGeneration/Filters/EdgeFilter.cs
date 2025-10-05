using NetTopologySuite.Geometries;
using VoronatorSharp;

namespace GraphGeneration.Filters;

public class EdgeFilter
{
    private readonly IReadOnlyCollection<NetTopologySuite.Geometries.Polygon> _ignore;
    private readonly float _expectedDistance;
    private readonly PointAllowedFilter _pointIgnoreFilter;

    public EdgeFilter(IReadOnlyCollection<NetTopologySuite.Geometries.Polygon> allowed, IReadOnlyCollection<NetTopologySuite.Geometries.Polygon> ignore, float hexSize)
    {
        _ignore = ignore;
        _expectedDistance = HexagonalGridGenerator.CalculateExpectedHexDistance(hexSize);
         _pointIgnoreFilter = new PointAllowedFilter(allowed);
    }

    public bool Skip(Vector2 a, Vector2 b)
    {
        if (_expectedDistance * 2 < Vector2.Distance(a, a))
        {
            return true;
        }
        
        // Создаем геометрическое представление ребра
        var lineString = new LineString([
            new Coordinate(a.x, a.y),
            new Coordinate(b.x, b.y)
        ]);

        // Проверяем, пересекает ли ребро любой из игнорируемых полигонов
        foreach (var polygon in _ignore)
        {
            if (lineString.Crosses(polygon) || polygon.Contains(lineString))
            {
                // Если ребро пересекает игнорируемый полигон, пропускаем его
                return true;
            }
        }
        
        // // Определяем, является ли ребро межполигональным
        // var polygon1 = GetPointPolygon(new Point(t1.x, t1.y), pointsByPolygon);
        // var polygon2 = GetPointPolygon(new Point(t2.x, t2.y), pointsByPolygon);
        // var isCrossPolygon = polygon1 == polygon2 && (polygon2 != null || polygon1 != null);

        return _pointIgnoreFilter.Skip(a) || _pointIgnoreFilter.Skip(b);
    }

    private static NetTopologySuite.Geometries.Polygon? GetPointPolygon(Point point,
        Dictionary<NetTopologySuite.Geometries.Polygon, List<Point>> pointsByPolygon)
    {
        foreach (var kvp in pointsByPolygon)
        {
            if (kvp.Value.Contains(point))
                return kvp.Key;
        }

        return null;
    }
}