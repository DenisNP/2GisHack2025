using VoronatorSharp;

namespace GraphGeneration.Filters;

public interface IPointFilter
{
    bool Skip(Vector2 vector);
}