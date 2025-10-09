using GraphGeneration.Geometry;
using NetTopologySuite.Geometries;
using VoronatorSharp;

namespace GraphGeneration.Filters;

public class EdgeCrossRestrictedFilter : IEdgeFilter
{
    private readonly IReadOnlyCollection<Polygon> _ignore;
    private readonly float _expectedDistance;
    private readonly PointAllowedFilter _pointAllowedFilter;

    public EdgeCrossRestrictedFilter(IReadOnlyCollection<Polygon> allowed, IReadOnlyCollection<Polygon> ignore, float hexSize)
    {
        _ignore = ignore;
        _expectedDistance = HexagonalGridGenerator.CalculateExpectedHexDistance(hexSize);
         _pointAllowedFilter = new PointAllowedFilter(allowed);
    }

    public bool Skip(Vector2 a, Vector2 b)
    {
        if (_expectedDistance * 1.5 < Vector2.Distance(a, a))
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
        
        return (!a.IsPoi && _pointAllowedFilter.Skip(a)) || (!b.IsPoi && _pointAllowedFilter.Skip(b));
    }
}