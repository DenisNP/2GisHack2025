using System.Collections.Generic;
using System.Linq;
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
            z.Region.Select(v => new Vector2((float)v.X, (float)v.Y)), z.ZoneType));

        List<Vector2> pp  =pois.Select(pp =>new Vector2(pp.Id, (float)pp.Point.X,  (float)pp.Point.Y, pp.Weight)).ToList();

        var result = GraphGenerator.Generate(polygons.ToList(), pp);
    
        return algorithm.GetBestWay(result);
    }
}