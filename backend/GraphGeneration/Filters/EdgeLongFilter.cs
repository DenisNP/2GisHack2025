using GraphGeneration.Models;
using NetTopologySuite.Geometries;
using VoronatorSharp;

namespace GraphGeneration.Filters;

public class EdgeLongFilter : IEdgeFilter 
{
    private readonly PolygonMap _polygonMap;
    private readonly float _hexSize;
    private IPointFilter _pointFilter;
    private readonly float _expectedDistance;

    public EdgeLongFilter(PolygonMap polygonMap, float hexSize)
    {
        _polygonMap = polygonMap;
        _expectedDistance = HexagonalGridGenerator.CalculateExpectedHexDistance(hexSize);
    }
    
    public bool Skip(Vector2 a, Vector2 b)
    {
        if (_expectedDistance * 2 < Vector2.Distance(a, b))
        {
            return true;
        }

        return false;
    }
}