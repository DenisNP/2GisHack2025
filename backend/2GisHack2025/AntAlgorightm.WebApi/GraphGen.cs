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
    public static Path GetBestPath(Zone[] zones, Poi[] poi, IAntColonyAlgorithm algorithm)
    {
        var polygons = zones
            .Select(zone => new Polygon(
            zone.Region.Select(region => new Vector2(region.Id, (float)region.X, (float)region.Y, 0)), zone.ZoneType)
            )
            .ToList();

        var points = poi.Select(pp => new Vector2(pp.Id, (float)pp.Point.X,  (float)pp.Point.Y, pp.Weight)).ToList();
        
        foreach (var zone in polygons)
        {
            zone.Vertices.Add(zone.Vertices.First());
        }

        var svg = GraphGenerator.GenerateSvg(polygons, points);
        
        File.WriteAllText("multi_polygon_graph.svg", svg, Encoding.UTF8);
        
        var edges = GraphGenerator.GenerateEdges(polygons, points);

        return algorithm.GetBestWay(edges);
    }
}