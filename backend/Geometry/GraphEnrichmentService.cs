

using System;
using System.Collections.Generic;
using System.Linq;

namespace Geometry;

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

        // Обрабатываем каждую зону
        foreach (var zone in zones.Where(z => z.Type != ZoneType.Restricted))
        {
            var zonePoints = GenerateZonePoints(zone, restrictedZones);
            allPoints.AddRange(zonePoints);
        }

        return allPoints.Distinct().ToList();
    }

    private List<Point> GenerateZonePoints(Zone zone, List<Zone> restrictedZones)
    {
        return zone.Type switch
        {
            ZoneType.Urban => GenerateUrbanPoints(zone, restrictedZones),
            ZoneType.Available when _config.UseDelaunayForAvailable 
                => GenerateAvailableDelaunayPoints(zone, restrictedZones),
            ZoneType.Available => GenerateAvailableGridPoints(zone, restrictedZones),
            _ => new List<Point>()
        };
    }

    private List<Point> GenerateUrbanPoints(Zone zone, List<Zone> restrictedZones)
    {
        var points = GenerateGridPointsForZone(zone, _config.GridStep);
        return FilterPoints(points, restrictedZones);
    }

    private List<Point> GenerateAvailableGridPoints(Zone zone, List<Zone> restrictedZones)
    {
        // Для Available зон используем более редкую сетку
        var points = GenerateGridPointsForZone(zone, _config.GridStep * 1.5);
        return FilterPoints(points, restrictedZones);
    }

    private List<Point> GenerateAvailableDelaunayPoints(Zone zone, List<Zone> restrictedZones)
    {
        var points = GeometryUtils.GenerateDelaunayPoints(zone.Region, 2.0);
        return FilterPoints(points, restrictedZones).Take(_config.MaxPointsPerZone).ToList();
    }

    private List<Point> GenerateGridPointsForZone(Zone zone, double step)
    {
        var (min, max) = GeometryUtils.GetBoundingBox(zone.Region);
        var gridPoints = GeometryUtils.GenerateGridPoints(min, max, step);
        
        return gridPoints
            .Where(point => GeometryUtils.IsPointInPolygon(point, zone.Region))
            .Take(_config.MaxPointsPerZone)
            .ToList();
    }

    private List<Point> FilterPoints(List<Point> points, List<Zone> restrictedZones)
    {
        return points
            .Where(point => !GeometryUtils.IsPointInRestrictedZone(point, restrictedZones))
            .ToList();
    }

    // Метод для вычисления "виртуального" расстояния с учетом нетерпеливости
    public double CalculateEffectiveDistance(Point a, Point b, ZoneType zoneType)
    {
        var realDistance = GeometryUtils.Distance(a, b);
        
        return zoneType switch
        {
            ZoneType.Urban => realDistance * _config.ImpatienceFactor, // Укороченное расстояние для тротуаров
            ZoneType.Available => realDistance, // Реальное расстояние для газонов
            _ => realDistance
        };
    }

    // Метод для выбора POI на основе весов
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