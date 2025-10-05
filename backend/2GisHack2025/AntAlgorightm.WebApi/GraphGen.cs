using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AntAlgorithm;
using AntAlgorithm.Abstractions;
using GraphGeneration;
using GraphGeneration.Models;
using VoronatorSharp;
using Path = AntAlgorithm.Path;

namespace WebApplication2;

public static class GraphGen
{
    public static IEnumerable<ResultEdge> GetBestPath(Zone[] zones, Poi[] poi, IAntColonyAlgorithm algorithm)
    {
        var maxId = poi.Max(p => p.Id);
        var polygons = zones
            .Select((zone, j) => new ZonePolygon(
                    zone.Region.Select((region, i) => new Vector2(maxId + i, (float)region.X, (float)region.Y, 0)),
                    zone.ZoneType
                )
            )
            .ToList();
                
        foreach (var zone in polygons)
        {
            zone.Vertices.Add(zone.Vertices.First());
        }

        var points = poi.Select(pp => new Vector2(pp.Id, (float)pp.Point.X,  (float)pp.Point.Y, pp.Weight)).ToList();

        var edges = GraphGenerator.GenerateEdges(polygons, points);

        return algorithm
            .GetAllWays(edges.Edges)
            .Select(e => new ResultEdge()
            {
                Weight = e.Weight,
                From = e.From.Point,
                To = e.To.Point,
            });
    }

    public static IEnumerable<ResultEdge> GetBestPath2(Zone[]  zones, Poi[] poi)
    {
        int maxId = poi.Max(p => p.Id);
        List<ZonePolygon> polygons = zones
            .Select((zone, j) => new ZonePolygon(
                    zone.Region.Select((region, i) => new Vector2(maxId + i, (float)region.X, (float)region.Y, 0)),
                    zone.ZoneType
                )
            )
            .ToList();
        
        foreach (var zone in polygons)
        {
            zone.Vertices.Add(zone.Vertices.First());
        }

        IEnumerable<GeomPoint> points = poi.Select(pp => new GeomPoint { Id = pp.Id, X = pp.Point.X, Y = pp.Point.Y, Weight = pp.Weight });
        var edges = LightGraphGenerator.GenerateEdges(polygons, points.ToList());

        return edges.Select(e => new ResultEdge
        {
            From = new Point
            {
                X = e.From.X,
                Y = e.From.Y,
            },
            To = new Point
            {
                X = e.To.X,
                Y = e.To.Y,
            },
            Weight = e.Weight,
        });
    }
}