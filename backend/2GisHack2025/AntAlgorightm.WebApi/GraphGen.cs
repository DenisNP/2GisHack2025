using System.IO;
using System.Linq;
using System.Text;
using AntAlgorithm;
using AntAlgorithm.Abstractions;
using GraphGeneration;
using VoronatorSharp;
using Path = AntAlgorithm.Path;

namespace WebApplication2;

public static class GraphGen
{
    public static Path GetBestPath(Zone[] zones, Poi[] poi, AntColonyAlgorithm2 algorithm)
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

        algorithm.Run(edges.Edges);

        return new Path();
    }
}