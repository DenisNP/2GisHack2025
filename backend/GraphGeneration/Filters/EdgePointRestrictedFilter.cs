using GraphGeneration.Geometry;
using NetTopologySuite.Geometries;
using VoronatorSharp;

namespace GraphGeneration.Filters;

public class EdgePointRestrictedFilter : IEdgeFilter
{
    private readonly float _expectedDistance;
    private readonly PointAllowedFilter _pointAllowedFilter;

    public EdgePointRestrictedFilter(IReadOnlyCollection<Polygon> allowed, IReadOnlyCollection<Polygon> _, float hexSize)
    {
        _expectedDistance = HexagonalGridGenerator.CalculateExpectedHexDistance(hexSize);
         _pointAllowedFilter = new PointAllowedFilter(allowed);
    }

    public bool Skip(Vector2 a, Vector2 b)
    {
        if (_expectedDistance * 1.5 < Vector2.Distance(a, a))
        {
            return true;
        }
        
        return (!a.IsPoi && _pointAllowedFilter.Skip(a)) || (!b.IsPoi && _pointAllowedFilter.Skip(b));
    }
}