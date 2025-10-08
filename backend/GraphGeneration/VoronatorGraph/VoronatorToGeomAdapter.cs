using GraphGeneration.Filters;
using GraphGeneration.Models;
using VoronatorSharp;

namespace GraphGeneration.VoronatorGraph;

public static class VoronatorToGeomAdapter
{
    public static (List<GeomPoint> Points, List<GeomEdge> Edges) ConvertToQuickGraph(
        int startId,
        PolygonMap polygonMap,
        Voronator voronoi,
        float hexSize)
    {
        var edgeFilter = new EdgeInRestrictedFilter(polygonMap, hexSize); 
        var points = new Dictionary<Vector2, GeomPoint>(voronoi.Delaunator.Points.Count);
        var edges = new List<GeomEdge>(voronoi.Delaunator.Points.Count);

        // Добавляем рёбра между соседними ячейками
        foreach (var edge in voronoi.Delaunator.GetEdges())
        {
            if (edgeFilter.Skip(edge.Item1, edge.Item2))
            {
                continue;
            }

            if (!points.TryGetValue(edge.Item1, out var from))
            {
                from = new GeomPoint(startId++, edge.Item1.X, edge.Item1.Y, edge.Item1.Weight);
                points.Add(edge.Item1, from);
            }
            else
            {
                from.IsCommon = true;
            }
            if (!points.TryGetValue(edge.Item2, out var to))
            {
                to = new GeomPoint(startId++, edge.Item2.X, edge.Item2.Y, edge.Item2.Weight);
                points.Add(edge.Item2, to);
            }
            else
            {
                to.IsCommon = true;
            }

            edges.Add(new GeomEdge(from, to));
        }
        
        return (points.Values.ToList(), edges);
    }
}