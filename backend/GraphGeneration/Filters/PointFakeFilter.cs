using GraphGeneration.Models;
using VoronatorSharp;

namespace GraphGeneration.Filters;

public class PointFakeFilter : IPointFilter 
{
    public PointFakeFilter(PolygonMap polygonMap)
    {
        
    }
    public bool Skip(Vector2 a)
    {
        return false;
    }
}