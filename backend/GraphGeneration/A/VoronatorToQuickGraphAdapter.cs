using GraphGeneration.Filters;
using QuickGraph;
using VoronatorSharp;

namespace GraphGeneration.A;

public class VoronatorToQuickGraphAdapter
{
    public static AdjacencyGraph<Vector2, Edge<Vector2>> ConvertToQuickGraph(
        IReadOnlyCollection<NetTopologySuite.Geometries.Polygon> ignore,
        IReadOnlyCollection<NetTopologySuite.Geometries.Polygon> allowed,
        Voronator voronoi,
        float hexSize)
    {
        var graph = new AdjacencyGraph<Vector2, Edge<Vector2>>();

        // var pointFilter = new PointIgnoreFilter(ignore);
        //
        // // Добавляем все точки Voronoi как вершины
        // foreach (var site in voronoi.Delaunator.Points)
        // {
        //     if (pointFilter.Skip(site))
        //     {
        //         continue;
        //     }
        //     graph.AddVertex(site);
        // }

        var edgeFilter = new EdgeFilter(allowed, ignore, hexSize); 

        // Добавляем рёбра между соседними ячейками
        foreach (var edge in voronoi.Delaunator.GetEdges())
        {
            if (edgeFilter.Skip(edge.Item1, edge.Item2))
            {
                continue;
            }
            
            graph.AddVertex(edge.Item1);
            graph.AddVertex(edge.Item2);
            graph.AddEdge(new Edge<Vector2>(edge.Item1, edge.Item2));
        }
        
        return graph;
    }
}