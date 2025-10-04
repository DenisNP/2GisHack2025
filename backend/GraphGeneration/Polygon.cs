// using System.Numerics;

using AntAlgorithm;
using VoronatorSharp;

namespace GraphGeneration;

public class Polygon
{
    public List<Vector2> Vertices { get; set; } = new List<Vector2>();
    
    public Polygon() { }
    
    public ZoneType Zone { get; set; }
    
    public Polygon(IEnumerable<Vector2> vertices, ZoneType zone = ZoneType.Available)
    {
        Vertices = vertices.Select((i,v) => new Vector2() { Id = v, x = i.X, y = i.y, Weight = 0 }).ToList();
        Zone = zone;
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