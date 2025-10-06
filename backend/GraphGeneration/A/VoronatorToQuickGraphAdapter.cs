using GraphGeneration.Filters;
using GraphGeneration.Models;
using QuickGraph;
using VoronatorSharp;

namespace GraphGeneration.A;

public class VoronatorToQuickGraphAdapter
{
    public static AdjacencyGraph<Vector2, Edge<Vector2>> ConvertToQuickGraph(
        PolygonMap polygonMap,
        Voronator voronoi,
        float hexSize)
    {
        var graph = new AdjacencyGraph<Vector2, Edge<Vector2>>();

        var edgeFilter = new EdgeInRestrictedFilter(polygonMap, hexSize); 
        var points = new HashSet<Vector2>(voronoi.Delaunator.Points.Count);

        // Добавляем рёбра между соседними ячейками
        foreach (var edge in voronoi.Delaunator.GetEdges())
        {
            if (edgeFilter.Skip(edge.Item1, edge.Item2))
            {
                continue;
            }

            if (!points.Contains(edge.Item1))
            {
                graph.AddVertex(edge.Item1);
            }
            
            if (!points.Contains(edge.Item2))
            {
                graph.AddVertex(edge.Item2);
            }
            
            points.Add(edge.Item1);
            points.Add(edge.Item2);

            graph.AddEdge(new Edge<Vector2>(edge.Item1, edge.Item2));
        }
        
        return graph;
    }
}