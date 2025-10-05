using GraphGeneration.Models;
using QuickGraph;
using VoronatorSharp;

namespace GraphGeneration.A;

public static class PointPairsHelper
{
    public static IEnumerable<(Vector2, Vector2)> GetUniquePairs(IReadOnlyCollection<Vector2> vectors)
    {
        return vectors
            .SelectMany((v1, i) => vectors
                .Skip(i + 1)
                .Select(v2 => (v1, v2)));
    }
    
    // Альтернативный вариант с Where
    public static List<(Vector2, Vector2)> GetUniquePairs(Vector2[] vectors)
    {
        return vectors
            .SelectMany((v1, i) => vectors
                .Where((v2, j) => i < j)
                .Select(v2 => (v1, v2)))
            .ToList();
    }
    
    public static IEnumerable<IEdge<Vector2>> GetEdges(IReadOnlyCollection<Vector2> vectors)
    {
        return vectors
            .Zip(vectors.Skip(1), (v1, v2) => new VoronatorFinderEdge(v1, v2));
    }
}