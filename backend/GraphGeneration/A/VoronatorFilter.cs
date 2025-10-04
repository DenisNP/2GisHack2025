using VoronatorSharp;

namespace GraphGeneration.A;

public static class VoronatorFilter
{
    public static (VoronoiEdgeFinder.Edge[], Vector2[]) Get(List<NetTopologySuite.Geometries.Polygon> ignore, Voronator voronoi, float hexSize, List<Vector2> shortPath)
    {
        var edgeFinder = new VoronoiEdgeFinder(voronoi, shortPath);

// Получение результатов для частичных точек
        var results = edgeFinder.FindNeighborsWithEdges(ignore, hexSize, shortPath);
            
        var edges = results
            .Values
            // .Select(d => (d.NeighborPoints, d.Edges))
            .SelectMany(d => d.Edges)
            .Distinct()
            .ToArray();
        
        var points  = results
            .Values
            // .Select(d => (d.NeighborPoints, d.Edges))
            .SelectMany(d => d.NeighborPoints)
            .Distinct()
            .ToArray();

        return (edges, points);
    }
}