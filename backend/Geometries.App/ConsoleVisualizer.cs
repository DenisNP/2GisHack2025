using Geometry;

namespace Geometries.App;

// ConsoleVisualizer.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class ConsoleVisualizer
{
    public static void VisualizeZonesAndPoints(List<Zone> zones, List<Point> points, int width = 80, int height = 40)
    {
        // Находим общий bounding box для всех данных
        var allPoints = zones.SelectMany(z => z.Region)
                           .Concat(points)
                           .ToList();
        
        if (!allPoints.Any()) return;

        var minX = allPoints.Min(p => p.X);
        var maxX = allPoints.Max(p => p.X);
        var minY = allPoints.Min(p => p.Y);
        var maxY = allPoints.Max(p => p.Y);

        // Создаём "холст"
        var canvas = new char[height, width];
        for (int i = 0; i < height; i++)
            for (int j = 0; j < width; j++)
                canvas[i, j] = ' ';

        // Функция для преобразования координат
        (int, int) TransformPoint(Point p)
        {
            int x = (int)((p.X - minX) / (maxX - minX) * (width - 1));
            int y = (int)((p.Y - minY) / (maxY - minY) * (height - 1));
            return (x, height - 1 - y); // инвертируем Y для консоли
        }

        // Рисуем зоны
        foreach (var zone in zones)
        {
            char zoneChar = zone.Type switch
            {
                ZoneType.Restricted => 'X',
                ZoneType.Urban => '=',
                ZoneType.Available => '.',
                _ => '?'
            };

            // Рисуем границы зоны
            for (int i = 0; i < zone.Region.Count; i++)
            {
                var p1 = zone.Region[i];
                var p2 = zone.Region[(i + 1) % zone.Region.Count];
                
                var (x1, y1) = TransformPoint(p1);
                var (x2, y2) = TransformPoint(p2);
                
                DrawLine(canvas, x1, y1, x2, y2, zoneChar);
            }

            // Заливаем внутренность зоны (опционально)
            if (zone.Type != ZoneType.Restricted)
            {
                var centroid = CalculateCentroid(zone.Region);
                var (cx, cy) = TransformPoint(centroid);
                FloodFill(canvas, cx, cy, zoneChar);
            }
        }

        // Рисуем точки
        foreach (var point in points)
        {
            var (x, y) = TransformPoint(point);
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                canvas[y, x] = '•';
            }
        }

        // Выводим в консоль
        Console.WriteLine("Визуализация зон и точек:");
        Console.WriteLine("X - Restricted, = - Urban, . - Available, • - точки графа");
        Console.WriteLine(new string('-', width));
        
        for (int i = 0; i < height; i++)
        {
            var line = new StringBuilder();
            for (int j = 0; j < width; j++)
            {
                line.Append(canvas[i, j]);
            }
            Console.WriteLine(line.ToString());
        }
        
        Console.WriteLine(new string('-', width));
        Console.WriteLine($"Всего точек: {points.Count}");
        Console.WriteLine($"Зоны: Urban={zones.Count(z => z.Type == ZoneType.Urban)}, " +
                         $"Available={zones.Count(z => z.Type == ZoneType.Available)}, " +
                         $"Restricted={zones.Count(z => z.Type == ZoneType.Restricted)}");
    }

    private static void DrawLine(char[,] canvas, int x1, int y1, int x2, int y2, char ch)
    {
        int dx = Math.Abs(x2 - x1);
        int dy = Math.Abs(y2 - y1);
        int sx = x1 < x2 ? 1 : -1;
        int sy = y1 < y2 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            if (x1 >= 0 && x1 < canvas.GetLength(1) && y1 >= 0 && y1 < canvas.GetLength(0))
            {
                canvas[y1, x1] = ch;
            }

            if (x1 == x2 && y1 == y2) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x1 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y1 += sy;
            }
        }
    }

    private static void FloodFill(char[,] canvas, int x, int y, char ch)
    {
        if (x < 0 || x >= canvas.GetLength(1) || y < 0 || y >= canvas.GetLength(0))
            return;

        if (canvas[y, x] != ' ')
            return;

        canvas[y, x] = ch;

        FloodFill(canvas, x + 1, y, ch);
        FloodFill(canvas, x - 1, y, ch);
        FloodFill(canvas, x, y + 1, ch);
        FloodFill(canvas, x, y - 1, ch);
    }

    private static Point CalculateCentroid(List<Point> polygon)
    {
        double sumX = 0, sumY = 0;
        foreach (var point in polygon)
        {
            sumX += point.X;
            sumY += point.Y;
        }
        return new Point(sumX / polygon.Count, sumY / polygon.Count);
    }
}