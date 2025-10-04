using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AntAlgorithm;
using AntAlgorithm.Abstractions;
using GraphGeneration;
using Microsoft.AspNetCore.Mvc;
using VoronatorSharp;
using Path = AntAlgorithm.Path;

namespace WebApplication2;

public static class GraphGen
{
    public static Path GetBestPath(Zone[] zones, Poi[] pois, IAntColonyAlgorithm algorithm)
    {
        var polygons = zones.Select(z => new Polygon(
            z.Region.Select(v => new Vector2((float)v.X, (float)v.Y)), z.ZoneType)).ToList();

        List<Vector2> pp  =pois.Select(pp =>new Vector2(pp.Id, (float)pp.Point.X,  (float)pp.Point.Y, pp.Weight)).ToList();

        foreach (var zone in polygons)
        {
            zone.Vertices.Add(zone.Vertices.First());
        }

        var result = GraphGenerator.Generate2(polygons, pp);
        
        File.WriteAllText("multi_polygon_graph.svg", result, Encoding.UTF8);

        return new Path(); //algorithm.GetBestWay(result);
    }
}