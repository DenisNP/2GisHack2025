using GraphGeneration.Models;
using NetTopologySuite.Geometries;
using PathScape.Domain.Models;

namespace GraphGeneration.Geometry;

public static class PolygonHelper
{
    public static PolygonMap GetPolygonMap(IReadOnlyCollection<ZonePolygon> zones)
    {
        var polygonsForGeneration = zones.Where(z => z.Type == ZoneType.Available).ToArray();
        var ignore  = new List<Polygon>(zones.Count);
        var allowed  = new List<Polygon>(zones.Count);
        var urban  = new List<Polygon>(zones.Count);
        var render  = new List<Polygon>(zones.Count);
        
        foreach (var zone in zones)
        {
            var coordinates = zone.Vertices
                .Select(v => new Coordinate(v.X, v.Y))
                .ToArray();
            var polygon = new Polygon(new LinearRing(coordinates));
            
            if (zone.Type == ZoneType.Restricted)
                ignore.Add(polygon);
            if (zone.Type == ZoneType.None)
                render.Add(polygon);
            if (zone.Type == ZoneType.Urban)
                urban.Add(polygon);
            if (zone.Type == ZoneType.Available)
                allowed.Add(polygon);
        }

        if (render.Count == 0)
        {
            render.AddRange(urban);
            render.AddRange(allowed);
        }
        
        return new PolygonMap(zones, polygonsForGeneration, urban, allowed, ignore, render);
    }

    public static bool IsPairCrossesAvailable(GeomPoint p1, GeomPoint p2, PolygonMap map)
    {
        var line = new LineString([new Coordinate(p1.X, p1.Y), new Coordinate(p2.X, p2.Y)]);
        foreach (Polygon polygon in map.Available)
        {
            if (line.Crosses(polygon))
            {
                return true;
            }
        }
        return false;
    }
}