using VoronatorSharp;

namespace GraphGeneration.Filters;

public interface IEdgeFilter
{
    bool Skip(Vector2 a, Vector2 b);
}