using GraphGeneration.Models;
using VoronatorSharp;

namespace GraphGeneration.A;

public static class VoronatorFilter
{
    public static (VoronatorFinderEdge[], Vector2[]) Get(List<NetTopologySuite.Geometries.Polygon> ignore, Voronator voronoi, float hexSize, IReadOnlyCollection<Vector2> shortPath)
    {
        var edgeFinder = new VoronotorEdgeFinder(voronoi);

        // Получение результатов для частичных точек
        var results = edgeFinder.FindNeighborsWithEdges(ignore, hexSize, shortPath);
            
        var edges = results
            .Values
            .SelectMany(d => d.Edges)
            .Distinct()
            .ToArray();
        
        var points  = results
            .Values
            .SelectMany(d => d.NeighborPoints)
            .Distinct()
            .ToArray();

        return (edges, points);
    }
}