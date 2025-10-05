using GraphGeneration.Models;
using VoronatorSharp;

namespace GraphGeneration.Filters;

public class EdgeFakeFilter : IEdgeFilter 
{
    public EdgeFakeFilter(PolygonMap polygonMap, float hexSize)
    {
        
    }
    public bool Skip(Vector2 a, Vector2 b)
    {
        return false;
    }
}