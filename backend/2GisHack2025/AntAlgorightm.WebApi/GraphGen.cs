using System.Collections.Generic;
using System.Linq;
using and.Models;
using AntAlgorithm;
using GraphGeneration;
using GraphGeneration.Models;
using VoronatorSharp;

namespace WebApplication2;

public static class GraphGen
{
    public static IEnumerable<ResultEdge> GetBestPath(Zone[] zones, Poi[] poi, AntColonyAlgorithm2 algorithm)
    {
        var maxId = poi.Max(p => p.Id);
        var polygons = zones
            .Select((zone, j) => new ZonePolygon(
            zone.Region.Select((region, i) => new Vector2(maxId + i, (float)region.X, (float)region.Y, 0)), zone.ZoneType)
            )
            .ToList();

        var points = poi.Select(pp => new Vector2(pp.Id, (float)pp.Point.X,  (float)pp.Point.Y, pp.Weight)).ToList();
        
        foreach (var zone in polygons)
        {
            zone.Vertices.Add(zone.Vertices.First());
        }

        var edges = GraphGenerator.GenerateEdges(polygons, points);

        var a = algorithm.Run(edges.Edges, edges.MaxLenPath * 3);

        return new List<ResultEdge>();
    }
}

public class ResultEdge
{
}