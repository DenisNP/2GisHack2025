using QuickGraph;
using QuickGraph.Algorithms;
using VoronatorSharp;

namespace GraphGeneration.A;

public static class VoronoiPathFinder
{
    public static List<Vector2> FindPath(AdjacencyGraph<Vector2, Edge<Vector2>> graph, Vector2 start, Vector2 end)
    {
        // Функция стоимости (евклидово расстояние)
        double EdgeCost(Edge<Vector2> edge) => Vector2.Distance(edge.Source, edge.Target);

        // Эвристическая функция (расстояние до цели)
        double Heuristic(Vector2 vertex) => Vector2.Distance(vertex, end);

        try
        {
            var tryGetPath  = graph.ShortestPathsAStar(EdgeCost, Heuristic, start);
            
            // // Находим путь
            // astar.Compute(start);
            
            if (tryGetPath(end, out IEnumerable<Edge<Vector2>> path))
            {
                return path.Select(edge => edge.Target).Prepend(start).ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Path not found: {ex.Message}");
        }
        
        return [];
    }
}