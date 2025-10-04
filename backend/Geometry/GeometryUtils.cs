using System;
using System.Collections.Generic;
using System.Linq;


namespace Geometry;

public static class GeometryUtils
{
    // Улучшенный алгоритм проверки точки в многоугольнике (Ray Casting)
    public static bool IsPointInPolygon(Point point, List<Point> polygon)
    {
        if (polygon.Count < 3)
            return false;

        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var pi = polygon[i];
            var pj = polygon[j];

            // Проверяем, лежит ли точка на ребре многоугольника
            if (IsPointOnSegment(point, pi, pj))
                return true;

            if (((pi.Y > point.Y) != (pj.Y > point.Y)) &&
                (point.X < (pj.X - pi.X) * (point.Y - pi.Y) / (pj.Y - pi.Y) + pi.X))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    // Проверка, лежит ли точка на отрезке
    public static bool IsPointOnSegment(Point point, Point segmentStart, Point segmentEnd)
    {
        // Проверяем коллинеарность
        double crossProduct = (point.Y - segmentStart.Y) * (segmentEnd.X - segmentStart.X) 
                            - (point.X - segmentStart.X) * (segmentEnd.Y - segmentStart.Y);
        
        if (Math.Abs(crossProduct) > 1e-10)
            return false;

        // Проверяем, что точка между концами отрезка
        double dotProduct = (point.X - segmentStart.X) * (segmentEnd.X - segmentStart.X) 
                          + (point.Y - segmentStart.Y) * (segmentEnd.Y - segmentStart.Y);
        
        if (dotProduct < 0)
            return false;

        double squaredLength = (segmentEnd.X - segmentStart.X) * (segmentEnd.X - segmentStart.X) 
                             + (segmentEnd.Y - segmentStart.Y) * (segmentEnd.Y - segmentStart.Y);
        
        return dotProduct <= squaredLength;
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

    // Улучшенная генерация точек для триангуляции Делоне
    public static List<Point> GenerateDelaunayPoints(List<Point> polygon, int targetPointCount)
    {
        var points = new List<Point>();
        
        if (polygon.Count == 0)
            return points;

        // Добавляем вершины многоугольника
        points.AddRange(polygon);

        // Если уже достаточно точек, возвращаем
        if (points.Count >= targetPointCount)
            return points.Take(targetPointCount).ToList();

        // Генерируем случайные точки внутри многоугольника
        var (min, max) = GetBoundingBox(polygon);
        var random = new Random();
        int attempts = 0;
        int maxAttempts = targetPointCount * 10; // Ограничиваем попытки

        while (points.Count < targetPointCount && attempts < maxAttempts)
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

    // Альтернативный метод: генерация точек по регулярной сетке с фильтрацией
    public static List<Point> GenerateGridPointsInPolygon(List<Point> polygon, double step)
    {
        var (min, max) = GetBoundingBox(polygon);
        var gridPoints = GenerateGridPoints(min, max, step);
        
        return gridPoints
            .Where(point => IsPointInPolygon(point, polygon))
            .ToList();
    }

    // Вычисление центра масс многоугольника
    public static Point CalculateCentroid(List<Point> polygon)
    {
        if (polygon.Count == 0)
            return new Point(0, 0);

        double sumX = 0;
        double sumY = 0;
        int count = polygon.Count;

        foreach (var point in polygon)
        {
            sumX += point.X;
            sumY += point.Y;
        }

        return new Point(sumX / count, sumY / count);
    }
}