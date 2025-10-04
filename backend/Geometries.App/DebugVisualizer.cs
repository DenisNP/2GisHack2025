using Geometry;

namespace Geometries.App;

// .cs
public static class DebugVisualizer
{
    public static void PrintZoneInfo(Zone zone, List<Point> allPoints)
    {
        var pointsInZone = allPoints.Count(p => GeometryUtils.IsPointInPolygon(p, zone.Region));
        Console.WriteLine($"Зона {zone.Id} ({zone.Type}):");
        Console.WriteLine($"  Вершин: {zone.Region.Count}");
        Console.WriteLine($"  Точек внутри: {pointsInZone}");
        Console.WriteLine($"  Bounding box: {GeometryUtils.GetBoundingBox(zone.Region)}");
    }

    public static void ValidateResults(List<Zone> zones, List<Point> points)
    {
        Console.WriteLine("\n=== ВАЛИДАЦИЯ РЕЗУЛЬТАТОВ ===");
        
        foreach (var zone in zones.Where(z => z.Type == ZoneType.Restricted))
        {
            var pointsInRestricted = points.Count(p => GeometryUtils.IsPointInPolygon(p, zone.Region));
            if (pointsInRestricted > 0)
            {
                Console.WriteLine($"⚠️  В restricted зоне {zone.Id} найдено {pointsInRestricted} точек!");
            }
        }

        foreach (var zone in zones.Where(z => z.Type != ZoneType.Restricted))
        {
            var generatedPoints = points.Count(p => GeometryUtils.IsPointInPolygon(p, zone.Region));
            Console.WriteLine($"Зона {zone.Id} ({zone.Type}): {generatedPoints} сгенерированных точек");
        }

        var orphanPoints = points.Where(p => 
            !zones.Any(z => z.Type != ZoneType.Restricted && GeometryUtils.IsPointInPolygon(p, z.Region))).ToList();
        
        if (orphanPoints.Any())
        {
            Console.WriteLine($"⚠️  Найдено {orphanPoints.Count} точек вне зон!");
        }
    }
}