using GraphGeneration.Models;
using VoronatorSharp;

namespace GraphGeneration.A;

public static class VoronatorNeighborsRecover
{
    public static (VoronatorFinderEdge[], Vector2[]) Get(     
        PolygonMap polygonMap,
        Voronator voronoi,
        float hexSize,
        IReadOnlyCollection<Vector2> shortPath)
    {
        var edgeFinder = new VoronatorNeighborsFinder(voronoi);

        // Получение результатов для частичных точек
        var results = edgeFinder.FindNeighborsWithEdges(polygonMap, hexSize, shortPath);
            
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