using System.Collections.Generic;
using System.Linq;
using GraphGeneration;
using GraphGeneration.Models;
using PathScape.Domain.Models;
using VoronatorSharp;
using WebApplication2.Dto;

namespace WebApplication2;

public static class PathScapeService
{
    public static void GenerateGraph(Zone[] zones, Poi[] poi)
    {
        var maxId = poi.Max(p => p.Id);
        var polygons = zones
            .Select(zone => new ZonePolygon(
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

        AStarVoronatorGraphGenerator.GenerateEdges(polygons, points);
    }

    public static IEnumerable<ResultPoint> RunSimulation(Zone[] zones, Poi[] poi)
    {
        int maxId = poi.Max(p => p.Id);
        List<ZonePolygon> polygons = zones
            .Select(zone => new ZonePolygon(
                    zone.Region.Select((region, i) => new Vector2(maxId + i, (float)region.X, (float)region.Y, 0)),
                    zone.ZoneType
                )
            )
            .ToList();
        
        foreach (var zone in polygons)
        {
            zone.Vertices.Add(zone.Vertices.First());
        }

        IEnumerable<GeomPoint> points = poi.Select(pp => new GeomPoint(pp.Id, (float)pp.Point.X, (float)pp.Point.Y, pp.Weight));
        var edges = LightGraphGenerator.GenerateEdges(polygons, points.ToList());

        return edges.Select(e => new ResultPoint
        {
            X = e.X,
            Y = e.Y,
            Weight = e.Influence,
        });
    }
}