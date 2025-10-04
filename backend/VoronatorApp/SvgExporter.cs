using VoronatorSharp;

namespace VoronatorApp;

public class SvgExporter
{
    public static void ExportToSvg2(List<List<Vector2>> polygons, List<Vector2> points, 
                                  List<Triangle> triangles, string filename)
    {
        if ((points == null || points.Count == 0) && 
            (polygons == null || polygons.Count == 0))
        {
            Console.WriteLine("Нет данных для экспорта");
            return;
        }
        
        // Собираем все точки для определения границ
        var allPoints = new List<Vector2>();
        if (points != null) allPoints.AddRange(points);
        foreach (var polygon in polygons)
        {
            allPoints.AddRange(polygon);
        }
        
        float margin = 20;
        float minX = allPoints.Min(p => p.x);
        float maxX = allPoints.Max(p => p.x);
        float minY = allPoints.Min(p => p.y);
        float maxY = allPoints.Max(p => p.y);
        
        float width = maxX - minX + margin * 2;
        float height = maxY - minY + margin * 2;
        
        using (var writer = new System.IO.StreamWriter(filename))
        {
            writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            writer.WriteLine($"<svg width=\"{width}\" height=\"{height}\" xmlns=\"http://www.w3.org/2000/svg\">");
            
            // Фон
            writer.WriteLine($"<rect width=\"100%\" height=\"100%\" fill=\"white\"/>");
            
            // Функция преобразования координат (Y инвертируется для SVG)
            string Transform(Vector2 p) => 
                $"{(p.x - minX + margin)},{(height - (p.y - minY + margin))}";
            
            // Рисуем треугольники (если есть)
            if (triangles != null && triangles.Count > 0)
            {
                writer.WriteLine("<!-- Triangles -->");
                foreach (var triangle in triangles)
                {
                    writer.WriteLine($"<polygon points=\"{Transform(triangle.Point1)} {Transform(triangle.Point2)} {Transform(triangle.Point3)}\" " +
                                   $"fill=\"lightblue\" stroke=\"blue\" stroke-width=\"1\" opacity=\"0.7\"/>");
                }
            }
            
            // Рисуем полигоны
            writer.WriteLine("<!-- Polygons -->");
            foreach (var polygon in polygons)
            {
                var pointsStr = string.Join(" ", polygon.Select(Transform));
                writer.WriteLine($"<polygon points=\"{pointsStr}\" fill=\"none\" stroke=\"red\" stroke-width=\"3\"/>");
            }
            
            // Рисуем точки (если есть)
            if (points != null && points.Count > 0)
            {
                writer.WriteLine("<!-- Points -->");
                foreach (var point in points)
                {
                    writer.WriteLine($"<circle cx=\"{Transform(point).Split(',')[0]}\" " +
                                   $"cy=\"{Transform(point).Split(',')[1]}\" r=\"2\" fill=\"black\"/>");
                }
            }
            
            // Координатная сетка для отладки (опционально)
            DrawGrid(writer, minX, maxX, minY, maxY, margin, width, height);
            
            writer.WriteLine("</svg>");
        }
        
        Console.WriteLine($"SVG сохранен в файл: {filename}");
        Console.WriteLine($"Размеры: {width}x{height}, точек: {points?.Count ?? 0}, треугольников: {triangles?.Count ?? 0}");
    }
    
    /// <summary>
    /// Рисует координатную сетку для отладки
    /// </summary>
    private static void DrawGrid(System.IO.StreamWriter writer, float minX, float maxX, float minY, float maxY, 
                               float margin, float width, float height)
    {
        writer.WriteLine("<!-- Grid -->");
        
        // Вертикальные линии
        for (float x = (float)Math.Ceiling(minX); x <= maxX; x += 50)
        {
            float svgX = x - minX + margin;
            writer.WriteLine($"<line x1=\"{svgX}\" y1=\"0\" x2=\"{svgX}\" y2=\"{height}\" " +
                           $"stroke=\"lightgray\" stroke-width=\"0.5\" opacity=\"0.5\"/>");
            writer.WriteLine($"<text x=\"{svgX}\" y=\"{height - 5}\" font-size=\"10\" fill=\"gray\">{x}</text>");
        }
        
        // Горизонтальные линии
        for (float y = (float)Math.Ceiling(minY); y <= maxY; y += 50)
        {
            float svgY = height - (y - minY + margin);
            writer.WriteLine($"<line x1=\"0\" y1=\"{svgY}\" x2=\"{width}\" y2=\"{svgY}\" " +
                           $"stroke=\"lightgray\" stroke-width=\"0.5\" opacity=\"0.5\"/>");
            writer.WriteLine($"<text x=\"5\" y=\"{svgY - 5}\" font-size=\"10\" fill=\"gray\">{y}</text>");
        }
        
        // Начало координат (0,0)
        float zeroX = -minX + margin;
        float zeroY = height - (-minY + margin);
        writer.WriteLine($"<circle cx=\"{zeroX}\" cy=\"{zeroY}\" r=\"3\" fill=\"green\"/>");
        writer.WriteLine($"<text x=\"{zeroX + 5}\" y=\"{zeroY - 5}\" font-size=\"12\" fill=\"green\">(0,0)</text>");
    }

    
    public static void ExportToSvg(List<List<Vector2>> polygons, List<Vector2> points, 
                                  List<Triangle> triangles, string filename)
    {
        var allPoints = points.Concat(polygons.SelectMany(p => p)).ToList();
        float margin = 20;
        float minX = allPoints.Min(p => p.x);
        float maxX = allPoints.Max(p => p.x);
        float minY = allPoints.Min(p => p.y);
        float maxY = allPoints.Max(p => p.y);
        
        float width = maxX - minX + margin * 2;
        float height = maxY - minY + margin * 2;
        
        using (var writer = new System.IO.StreamWriter(filename))
        {
            writer.WriteLine($"<svg width=\"{width}\" height=\"{height}\" xmlns=\"http://www.w3.org/2000/svg\">");
            
            // Трансформация координат
            string Transform(Vector2 p) => 
                $"{(p.x - minX + margin)},{(p.y - minY + margin)}";
            
            // Треугольники
            if (triangles != null)
            {
                foreach (var triangle in triangles)
                {
                    writer.WriteLine($"<polygon points=\"{Transform(triangle.Point1)} {Transform(triangle.Point2)} {Transform(triangle.Point3)}\" " +
                                   $"fill=\"lightblue\" stroke=\"blue\" stroke-width=\"1\" opacity=\"0.5\"/>");
                }
            }
            
            // Полигоны
            foreach (var polygon in polygons)
            {
                var pointsStr = string.Join(" ", polygon.Select(Transform));
                writer.WriteLine($"<polygon points=\"{pointsStr}\" fill=\"none\" stroke=\"red\" stroke-width=\"3\"/>");
            }
            
            // Точки
            foreach (var point in points)
            {
                writer.WriteLine($"<circle cx=\"{Transform(point).Split(',')[0]}\" " +
                               $"cy=\"{Transform(point).Split(',')[1]}\" r=\"2\" fill=\"black\"/>");
            }
            
            writer.WriteLine("</svg>");
        }
        
        Console.WriteLine($"SVG сохранен в файл: {filename}");
    }
}

// Использование:
// SvgExporter.ExportToSvg(polygons, points, triangles, "delaunay.svg");