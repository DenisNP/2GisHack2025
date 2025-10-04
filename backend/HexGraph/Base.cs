

using System.Numerics;

namespace HexGraph;

public class Polygon
{
    public List<Vector2> Vertices { get; set; } = new List<Vector2>();
    
    public Polygon() { }
    
    public Polygon(IEnumerable<Vector2> vertices)
    {
        Vertices = vertices.ToList();
    }
    
    // Проверка, находится ли точка внутри полигона (алгоритм winding number)
    public bool ContainsPoint(Vector2 point)
    {
        int windingNumber = 0;
        int n = Vertices.Count;
        
        for (int i = 0; i < n; i++)
        {
            Vector2 current = Vertices[i];
            Vector2 next = Vertices[(i + 1) % n];
            
            if (current.Y <= point.Y)
            {
                if (next.Y > point.Y && IsLeft(current, next, point) > 0)
                    windingNumber++;
            }
            else
            {
                if (next.Y <= point.Y && IsLeft(current, next, point) < 0)
                    windingNumber--;
            }
        }
        
        return windingNumber != 0;
    }
    
    private float IsLeft(Vector2 a, Vector2 b, Vector2 point)
    {
        return (b.X - a.X) * (point.Y - a.Y) - (point.X - a.X) * (b.Y - a.Y);
    }
    
    // Получить ограничивающий прямоугольник
    public (Vector2 min, Vector2 max) GetBoundingBox()
    {

            
        float minX = Vertices.Min(v => v.X);
        float minY = Vertices.Min(v => v.Y);
        float maxX = Vertices.Max(v => v.X);
        float maxY = Vertices.Max(v => v.Y);
        
        return (new Vector2(minX, minY), new Vector2(maxX, maxY));
    }
}

public class PointGenerator
{
    public static List<Vector2> GeneratePointsInPolygon(Polygon polygon, float pointSpacing)
    {
        var points = new List<Vector2>();
        var (min, max) = polygon.GetBoundingBox();
        
        for (float x = min.X; x <= max.X; x += pointSpacing)
        {
            for (float y = min.Y; y <= max.Y; y += pointSpacing)
            {
                var point = new Vector2(x, y);
                if (polygon.ContainsPoint(point))
                {
                    points.Add(point);
                }
            }
        }
        
        return points;
    }
    
    // Альтернативный метод: случайные точки с равномерным распределением
    public static List<Vector2> GenerateRandomPointsInPolygon(Polygon polygon, int pointCount, Random random = null)
    {
        random ??= new Random();
        var points = new List<Vector2>();
        var (min, max) = polygon.GetBoundingBox();
        
        while (points.Count < pointCount)
        {
            float x = min.X + (float)random.NextDouble() * (max.X - min.X);
            float y = min.Y + (float)random.NextDouble() * (max.Y - min.Y);
            
            var point = new Vector2(x, y);
            if (polygon.ContainsPoint(point))
            {
                points.Add(point);
            }
        }
        
        return points;
    }
}