using Geometry;

namespace Geometries.App;

// .cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class HtmlVisualizer
{
    public static string GenerateHtmlVisualization(List<Zone> zones, List<Point> points, int width = 800, int height = 600)
    {
        var allPoints = zones.SelectMany(z => z.Region).Concat(points).ToList();
        if (!allPoints.Any()) return string.Empty;

        var minX = allPoints.Min(p => p.X);
        var maxX = allPoints.Max(p => p.X);
        var minY = allPoints.Min(p => p.Y);
        var maxY = allPoints.Max(p => p.Y);

        var scaleX = width / (maxX - minX);
        var scaleY = height / (maxY - minY);
        var scale = Math.Min(scaleX, scaleY) * 0.9;

        // Функция преобразования координат
        (double, double) TransformPoint(Point p)
        {
            double x = (p.X - minX) * scale + (width - (maxX - minX) * scale) / 2;
            double y = (p.Y - minY) * scale + (height - (maxY - minY) * scale) / 2;
            return (x, y);
        }

        var svg = new StringBuilder();
        svg.AppendLine($@"<svg width=""{width}"" height=""{height}"" xmlns=""http://www.w3.org/2000/svg"">");
        
        // Рисуем зоны
        foreach (var zone in zones)
        {
            string fill = zone.Type switch
            {
                ZoneType.Restricted => "rgba(255,0,0,0.3)",
                ZoneType.Urban => "rgba(255,165,0,0.3)",
                ZoneType.Available => "rgba(0,255,0,0.3)",
                _ => "gray"
            };

            string stroke = zone.Type switch
            {
                ZoneType.Restricted => "red",
                ZoneType.Urban => "orange",
                ZoneType.Available => "green",
                _ => "gray"
            };

            var pointsStr = string.Join(" ", zone.Region.Select(p => 
            {
                var (x, y) = TransformPoint(p);
                return $"{x:F1},{y:F1}";
            }));

            svg.AppendLine($@"<polygon points=""{pointsStr}"" fill=""{fill}"" stroke=""{stroke}"" stroke-width=""2"" />");
            
            // Подписываем зоны
            var centroid = CalculateCentroid(zone.Region);
            var (cx, cy) = TransformPoint(centroid);
            svg.AppendLine($@"<text x=""{cx}"" y=""{cy}"" text-anchor=""middle"" font-size=""12"">{zone.Type}</text>");
        }

        // Рисуем точки
        foreach (var point in points)
        {
            var (x, y) = TransformPoint(point);
            svg.AppendLine($@"<circle cx=""{x}"" cy=""{y}"" r=""3"" fill=""blue"" />");
        }

        // Рисуем POI точки (если есть)
        var poiPoints = points.Take(2); // Пример: первые 2 точки как POI
        foreach (var point in poiPoints)
        {
            var (x, y) = TransformPoint(point);
            svg.AppendLine($@"<circle cx=""{x}"" cy=""{y}"" r=""5"" fill=""purple"" stroke=""black"" stroke-width=""1"" />");
        }

        svg.AppendLine("</svg>");

        string html = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <title>Визуализация графа</title>
            <style>
                body {{ font-family: Arial, sans-serif; margin: 20px; }}
                .legend {{ margin-bottom: 20px; }}
                .legend-item {{ display: inline-block; margin-right: 20px; }}
                .color-box {{ display: inline-block; width: 20px; height: 20px; margin-right: 5px; }}
            </style>
        </head>
        <body>
            <h1>Визуализация обогащенного графа</h1>
            <div class='legend'>
                <div class='legend-item'><div class='color-box' style='background: rgba(255,0,0,0.3);'></div>Restricted</div>
                <div class='legend-item'><div class='color-box' style='background: rgba(255,165,0,0.3);'></div>Urban</div>
                <div class='legend-item'><div class='color-box' style='background: rgba(0,255,0,0.3);'></div>Available</div>
                <div class='legend-item'><div class='color-box' style='background: blue;'></div>Точки графа</div>
                <div class='legend-item'><div class='color-box' style='background: purple;'></div>POI</div>
            </div>
            {svg}
            <div>Всего точек: {points.Count}</div>
        </body>
        </html>";

        return html;
    }

    public static void SaveHtmlToFile(string html, string filePath = "visualization.html")
    {
        System.IO.File.WriteAllText(filePath, html);
        Console.WriteLine($"Визуализация сохранена в файл: {filePath}");
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