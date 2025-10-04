using VoronatorSharp;

namespace VoronatorApp;

public class SvgExporter
{
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