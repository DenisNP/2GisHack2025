using Geometry;
using Path = System.IO.Path;

namespace Geometries.App;


public static class FileExportService
{
    public static void ExportToFiles(List<Zone> zones, List<Point> graphPoints, List<Poi> pois, string basePath = "export")
    {
        Directory.CreateDirectory(basePath);

        // WKT
        var wkt = GeoFormatsGenerator.GenerateWkt(zones, graphPoints, pois);
        File.WriteAllText(Path.Combine(basePath, "map_data.wkt"), wkt);
        Console.WriteLine($"WKT файл сохранен: {Path.Combine(basePath, "map_data.wkt")}");

        // GeoJSON
        var geojson = GeoFormatsGenerator.GenerateGeoJson(zones, graphPoints, pois);
        File.WriteAllText(Path.Combine(basePath, "map_data.geojson"), geojson);
        Console.WriteLine($"GeoJSON файл сохранен: {Path.Combine(basePath, "map_data.geojson")}");

        // KML
        var kml = GeoFormatsGenerator.GenerateKml(zones, graphPoints, pois);
        File.WriteAllText(Path.Combine(basePath, "map_data.kml"), kml);
        Console.WriteLine($"KML файл сохранен: {Path.Combine(basePath, "map_data.kml")}");

        // GPX
        var gpx = GeoFormatsGenerator.GenerateGpx(zones, graphPoints, pois);
        File.WriteAllText(Path.Combine(basePath, "map_data.gpx"), gpx);
        Console.WriteLine($"GPX файл сохранен: {Path.Combine(basePath, "map_data.gpx")}");

        // CSV для простого анализа
        GenerateCsvFiles(zones, graphPoints, pois, basePath);
    }

    private static void GenerateCsvFiles(List<Zone> zones, List<Point> graphPoints, List<Poi> pois, string basePath)
    {
        // Зоны
        var zoneCsv = new List<string> { "ZoneId,Type,VertexIndex,X,Y" };
        foreach (var zone in zones)
        {
            for (int i = 0; i < zone.Region.Count; i++)
            {
                zoneCsv.Add($"{zone.Id},{zone.Type},{i},{zone.Region[i].X},{zone.Region[i].Y}");
            }
        }
        File.WriteAllLines(Path.Combine(basePath, "zones.csv"), zoneCsv);

        // Точки графа
        var pointsCsv = new List<string> { "PointIndex,X,Y,Type" };
        for (int i = 0; i < graphPoints.Count; i++)
        {
            pointsCsv.Add($"{i},{graphPoints[i].X},{graphPoints[i].Y},GRAPH");
        }
        File.WriteAllLines(Path.Combine(basePath, "graph_points.csv"), pointsCsv);

        // POI
        var poiCsv = new List<string> { "PoiId,X,Y,Weight" };
        foreach (var poi in pois)
        {
            poiCsv.Add($"{poi.Id},{poi.Point.X},{poi.Point.Y},{poi.Weight}");
        }
        File.WriteAllLines(Path.Combine(basePath, "poi.csv"), poiCsv);
    }
}