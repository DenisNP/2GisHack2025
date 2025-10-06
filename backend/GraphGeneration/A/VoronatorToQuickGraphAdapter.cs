using GraphGeneration.Filters;
using GraphGeneration.Models;
using VoronatorSharp;

namespace GraphGeneration.A;

public static class VoronatorToQuickGraphAdapter
{
    public static (List<GeomPoint> Points, List<GeomEdge> Edges) ConvertToQuickGraph(
        PolygonMap polygonMap,
        Voronator voronoi,
        float hexSize)
    {
        var edgeFilter = new EdgeInRestrictedFilter(polygonMap, hexSize); 
        var points = new HashSet<GeomPoint>(voronoi.Delaunator.Points.Count);
        var edges = new List<GeomEdge>(voronoi.Delaunator.Points.Count);

        // Добавляем рёбра между соседними ячейками
        foreach (var edge in voronoi.Delaunator.GetEdges())
        {
            if (edgeFilter.Skip(edge.Item1, edge.Item2))
            {
                continue;
            }
            var from = new GeomPoint(edge.Item1);
            var to = new GeomPoint(edge.Item2);
            points.Add(from);
            points.Add(to);

            edges.Add(new GeomEdge(from, to, 0));
        }
        
        return (points.ToList(), edges);
    }
}