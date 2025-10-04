using System.Numerics;
using System.Text;

namespace HexGraph;

public class HexagonalGridVisualizer
{
    public static string CreateHexagonalGridSvg(
        List<Polygon> sourcePolygons,
        List<Vector2> hexPoints,
        float hexSize,
        bool showGrid = true)
    {
        var svg = new StringBuilder();
        svg.AppendLine(@"<svg width=""800"" height=""600"" xmlns=""http://www.w3.org/2000/svg"">");
        svg.AppendLine(@"<rect width=""100%"" height=""100%"" fill=""#1a1a1a""/>");
        
        // Рисуем исходные полигоны
        foreach (var polygon in sourcePolygons)
        {
            svg.Append(@"<polygon points=""");
            foreach (var vertex in polygon.Vertices)
            {
                svg.Append($"{vertex.X},{vertex.Y} ");
            }
            svg.AppendLine(@""" fill=""none"" stroke=""#4ecdc4"" stroke-width=""2""/>");
        }
        
        // Рисуем гексагональную сетку (опционально)
        if (showGrid && hexPoints.Count > 0)
        {
            foreach (var center in hexPoints)
            {
                DrawHexagon(svg, center, hexSize, "#3498db", 0.3f);
            }
        }
        
        // Рисуем центры шестиугольников
        foreach (var point in hexPoints)
        {
            svg.AppendLine($@"<circle cx=""{point.X}"" cy=""{point.Y}"" r=""2"" fill=""#ff6b6b""/>");
        }
        
        svg.AppendLine("</svg>");
        return svg.ToString();
    }
    
    private static void DrawHexagon(StringBuilder svg, Vector2 center, float size, string color, float opacity)
    {
        var points = CalculateHexagonVertices(center, size);
        
        svg.Append(@"<polygon points=""");
        foreach (var point in points)
        {
            svg.Append($"{point.X},{point.Y} ");
        }
        svg.AppendLine($@""" fill=""{color}"" fill-opacity=""{opacity}"" stroke=""{color}"" stroke-width=""0.5""/>");
    }
    
    private static List<Vector2> CalculateHexagonVertices(Vector2 center, float size)
    {
        var vertices = new List<Vector2>();
        
        for (int i = 0; i < 6; i++)
        {
            float angle = (float)(Math.PI / 3 * i); // 60 градусов
            float x = center.X + size * (float)Math.Cos(angle);
            float y = center.Y + size * (float)Math.Sin(angle);
            vertices.Add(new Vector2(x, y));
        }
        
        return vertices;
    }
}