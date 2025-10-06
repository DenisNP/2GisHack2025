using GraphGeneration.Models;

namespace GraphGeneration.A;

public static class PointPairsHelper
{
    public static IEnumerable<(GeomPoint, GeomPoint)> GetUniquePairs(IReadOnlyCollection<GeomPoint> vectors)
    {
        for (int i = 0; i < vectors.Count; i++)
        {
            for (int j = i + 1; j < vectors.Count; j++)
            {
                yield return (vectors.ElementAt(i), vectors.ElementAt(j));
            }
        }
    }
    
   
    public static IEnumerable<IEdge<GeomPoint>> GetEdges(IReadOnlyCollection<GeomPoint> vectors)
    {
        return vectors
            .Zip(vectors.Skip(1), (v1, v2) => new GeomEdge(v1, v2, 0));
    }
}