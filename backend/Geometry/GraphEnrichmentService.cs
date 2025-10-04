

using System;
using System.Collections.Generic;
using System.Linq;

namespace Geometry;
// GraphEnrichmentService.cs - исправленная версия
public class GraphEnrichmentService
{
    private readonly GraphEnrichmentConfig _config;

    public GraphEnrichmentService(GraphEnrichmentConfig config = null)
    {
        _config = config ?? new GraphEnrichmentConfig();
    }

    public List<Point> EnrichGraph(List<Zone> zones, List<Poi> pois)
    {
        var allPoints = new List<Point>();
        var restrictedZones = zones.Where(z => z.Type == ZoneType.Restricted).ToList();

        // Добавляем POI точки
        allPoints.AddRange(pois.Select(p => p.Point));

        // Обрабатываем каждую зону (кроме Restricted)
        foreach (var zone in zones.Where(z => z.Type != ZoneType.Restricted))
        {
            Console.WriteLine($"Обработка зоны {zone.Id} типа {zone.Type}...");
            var zonePoints = GenerateZonePoints(zone, restrictedZones);
            Console.WriteLine($"Сгенерировано {zonePoints.Count} точек для зоны {zone.Id}");
            allPoints.AddRange(zonePoints);
        }

        // Удаляем дубликаты (с учетом погрешности)
        return RemoveDuplicatePoints(allPoints);
    }

    private List<Point> GenerateZonePoints(Zone zone, List<Zone> restrictedZones)
    {
        try
        {
            List<Point> zonePoints = zone.Type switch
            {
                ZoneType.Urban => GenerateUrbanPoints(zone, restrictedZones),
                ZoneType.Available when _config.UseDelaunayForAvailable 
                    => GenerateAvailableDelaunayPoints(zone, restrictedZones),
                ZoneType.Available => GenerateAvailableGridPoints(zone, restrictedZones),
                _ => new List<Point>()
            };

            Console.WriteLine($"Для зоны {zone.Id} ({zone.Type}) сгенерировано {zonePoints.Count} точек");
            return zonePoints;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при генерации точек для зоны {zone.Id}: {ex.Message}");
            return new List<Point>();
        }
    }

    private List<Point> GenerateUrbanPoints(Zone zone, List<Zone> restrictedZones)
    {
        // Для Urban используем плотную сетку
        var points = GenerateGridPointsForZone(zone, _config.GridStep);
        var filtered = FilterPoints(points, restrictedZones);
        Console.WriteLine($"Urban зона {zone.Id}: {points.Count} -> {filtered.Count} после фильтрации");
        return filtered;
    }

    private List<Point> GenerateAvailableGridPoints(Zone zone, List<Zone> restrictedZones)
    {
        // Для Available зон используем более редкую сетку
        var points = GenerateGridPointsForZone(zone, _config.GridStep * 1.5);
        var filtered = FilterPoints(points, restrictedZones);
        Console.WriteLine($"Available зона {zone.Id} (grid): {points.Count} -> {filtered.Count} после фильтрации");
        return filtered.Take(_config.MaxPointsPerZone).ToList();
    }

    private List<Point> GenerateAvailableDelaunayPoints(Zone zone, List<Zone> restrictedZones)
    {
        // Для Available зон генерируем точки через улучшенный метод
        var points = GenerateEnhancedDelaunayPoints(zone.Region, _config.MaxPointsPerZone);
        var filtered = FilterPoints(points, restrictedZones);
        Console.WriteLine($"Available зона {zone.Id} (delaunay): {points.Count} -> {filtered.Count} после фильтрации");
        return filtered;
    }

    private List<Point> GenerateGridPointsForZone(Zone zone, double step)
    {
        var (min, max) = GeometryUtils.GetBoundingBox(zone.Region);
        
        // Добавляем небольшую погрешность чтобы избежать проблем с границами
        var adjustedMin = new Point(min.X - 0.001, min.Y - 0.001);
        var adjustedMax = new Point(max.X + 0.001, max.Y + 0.001);
        
        var gridPoints = GeometryUtils.GenerateGridPoints(adjustedMin, adjustedMax, step);
        
        var pointsInPolygon = gridPoints
            .Where(point => GeometryUtils.IsPointInPolygon(point, zone.Region))
            .Take(_config.MaxPointsPerZone)
            .ToList();

        Console.WriteLine($"Сетка для зоны {zone.Id}: {gridPoints.Count} -> {pointsInPolygon.Count} в полигоне");
        return pointsInPolygon;
    }

    private List<Point> GenerateEnhancedDelaunayPoints(List<Point> polygon, int targetPointCount)
    {
        var points = new List<Point>();
        
        if (polygon.Count == 0)
            return points;

        // Всегда добавляем вершины многоугольника
        points.AddRange(polygon);

        // Если нужно больше точек, генерируем случайные
        if (points.Count < targetPointCount)
        {
            var (min, max) = GeometryUtils.GetBoundingBox(polygon);
            var random = new Random();
            int attempts = 0;
            int maxAttempts = targetPointCount * 5;

            while (points.Count < targetPointCount && attempts < maxAttempts)
            {
                double x = min.X + random.NextDouble() * (max.X - min.X);
                double y = min.Y + random.NextDouble() * (max.Y - min.Y);
                var point = new Point(x, y);

                if (GeometryUtils.IsPointInPolygon(point, polygon))
                {
                    points.Add(point);
                }
                attempts++;
            }
        }

        Console.WriteLine($"Delaunay points: сгенерировано {points.Count} точек");
        return points;
    }

    private List<Point> FilterPoints(List<Point> points, List<Zone> restrictedZones)
    {
        return points
            .Where(point => !GeometryUtils.IsPointInRestrictedZone(point, restrictedZones))
            .ToList();
    }

    private List<Point> RemoveDuplicatePoints(List<Point> points, double tolerance = 0.001)
    {
        var uniquePoints = new List<Point>();
        
        foreach (var point in points)
        {
            if (!uniquePoints.Any(p => 
                Math.Abs(p.X - point.X) < tolerance && 
                Math.Abs(p.Y - point.Y) < tolerance))
            {
                uniquePoints.Add(point);
            }
        }
        
        return uniquePoints;
    }

    // Остальные методы без изменений...
    public double CalculateEffectiveDistance(Point a, Point b, ZoneType zoneType)
    {
        var realDistance = GeometryUtils.Distance(a, b);
        
        return zoneType switch
        {
            ZoneType.Urban => realDistance * _config.ImpatienceFactor,
            ZoneType.Available => realDistance,
            _ => realDistance
        };
    }

    public Poi SelectPoiByWeight(List<Poi> pois, Random random = null)
    {
        random ??= new Random();
        
        if (pois.Count == 0) return null;
        if (pois.Count == 1) return pois[0];

        var totalWeight = pois.Sum(p => p.Weight);
        var randomValue = random.NextDouble() * totalWeight;
        
        double cumulative = 0;
        foreach (var poi in pois)
        {
            cumulative += poi.Weight;
            if (randomValue <= cumulative)
                return poi;
        }

        return pois.Last();
    }
}