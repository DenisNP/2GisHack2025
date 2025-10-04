using System;
using System.Collections.Generic;
using System.Linq;


namespace Geometry;


public static class GeometryUtils
{
    // Алгоритм проверки точки в многоугольнике (Ray Casting)
    public static bool IsPointInPolygon(Point point, List<Point> polygon)
    {
        if (polygon.Count < 3)
            return false;

        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var pi = polygon[i];
            var pj = polygon[j];

            if (((pi.Y > point.Y) != (pj.Y > point.Y)) &&
                (point.X < (pj.X - pi.X) * (point.Y - pi.Y) / (pj.Y - pi.Y) + pi.X))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    // Проверка, находится ли точка в любой из restricted зон
    public static bool IsPointInRestrictedZone(Point point, List<Zone> restrictedZones)
    {
        return restrictedZones.Any(zone => IsPointInPolygon(point, zone.Region));
    }

    // Вычисление ограничивающего прямоугольника для многоугольника
    public static (Point min, Point max) GetBoundingBox(List<Point> polygon)
    {
        if (polygon.Count == 0)
            return (new Point(0, 0), new Point(0, 0));

        double minX = polygon[0].X;
        double minY = polygon[0].Y;
        double maxX = polygon[0].X;
        double maxY = polygon[0].Y;

        foreach (var point in polygon)
        {
            minX = Math.Min(minX, point.X);
            minY = Math.Min(minY, point.Y);
            maxX = Math.Max(maxX, point.X);
            maxY = Math.Max(maxY, point.Y);
        }

        return (new Point(minX, minY), new Point(maxX, maxY));
    }

    // Вычисление расстояния между двумя точками
    public static double Distance(Point a, Point b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    // Генерация равномерной сетки точек внутри bounding box
    public static List<Point> GenerateGridPoints(Point min, Point max, double step)
    {
        var points = new List<Point>();
        
        for (double x = min.X; x <= max.X; x += step)
        {
            for (double y = min.Y; y <= max.Y; y += step)
            {
                points.Add(new Point(x, y));
            }
        }

        return points;
    }

    // Простая реализация триангуляции Делоне - генерация дополнительных точек
    public static List<Point> GenerateDelaunayPoints(List<Point> polygon, double density)
    {
        var points = new List<Point>();
        
        // Добавляем вершины многоугольника
        points.AddRange(polygon);

        // Генерируем случайные точки внутри многоугольника
        var (min, max) = GetBoundingBox(polygon);
        var random = new Random();
        int attempts = 0;
        int maxAttempts = 1000;

        while (points.Count < polygon.Count * density && attempts < maxAttempts)
        {
            double x = min.X + random.NextDouble() * (max.X - min.X);
            double y = min.Y + random.NextDouble() * (max.Y - min.Y);
            var point = new Point(x, y);

            if (IsPointInPolygon(point, polygon))
            {
                points.Add(point);
            }
            attempts++;
        }

        return points;
    }
}