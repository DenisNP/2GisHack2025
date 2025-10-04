using VoronatorSharp;

namespace GraphGeneration.A;

public static class UniquePairsLinq
{
    public static List<(Vector2, Vector2)> GetUniquePairsLinq(IReadOnlyCollection<Vector2> vectors)
    {
        return vectors
            .SelectMany((v1, i) => vectors
                .Skip(i)
                .Select(v2 => (v1, v2)))
            .ToList();
    }
    
    // Альтернативный вариант с Where
    public static List<(Vector2, Vector2)> GetUniquePairsLinq2(Vector2[] vectors)
    {
        return vectors
            .SelectMany((v1, i) => vectors
                .Where((v2, j) => i < j)
                .Select(v2 => (v1, v2)))
            .ToList();
    }
}