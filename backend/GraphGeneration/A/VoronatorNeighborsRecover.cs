using GraphGeneration.Models;
using VoronatorSharp;

namespace GraphGeneration.A;

public static class VoronatorNeighborsRecover
{
    public static (VoronatorFinderEdge[], Vector2[]) Get(     
        IReadOnlyCollection<NetTopologySuite.Geometries.Polygon> ignore,
        IReadOnlyCollection<NetTopologySuite.Geometries.Polygon> allowed,
        Voronator voronoi,
        float hexSize,
        IReadOnlyCollection<Vector2> shortPath)
    {
        var edgeFinder = new VoronatorNeighborsFinder(voronoi);

        // Получение результатов для частичных точек
        var results = edgeFinder.FindNeighborsWithEdges(ignore, allowed, hexSize, shortPath);
            
        var edges = results
            .Values
            .SelectMany(d => d.Edges)
            .Distinct()
            .ToArray();
        
        var points  = results
            .Values
            .SelectMany(d => d.Edges)
            .SelectMany(p => p.Points)
            .Distinct()
            .ToArray();

        return (edges, points);
    }
}