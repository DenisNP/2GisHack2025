// using System.Numerics;

using VoronatorSharp;

namespace GraphGeneration;

public class Polygon
{
    public List<Poi> Vertices { get; set; } = new List<Poi>();
    
    public Polygon() { }
    
    public Polygon(IEnumerable<Vector2> vertices)
    {
        Vertices = vertices.Select((i,v) => new Poi() { Id = v, Point = i, Weight = 0 }).ToList();
    }
    
    // Проверка, находится ли точка внутри полигона (алгоритм winding number)
    public bool ContainsPoint(Vector2 point)
    {
        int windingNumber = 0;
        int n = Vertices.Count;
        
        for (int i = 0; i < n; i++)
        {
            Vector2 current = Vertices[i].Point;
            Vector2 next = Vertices[(i + 1) % n].Point;
            
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

            
        float minX = Vertices.Min(v => v.Point.X);
        float minY = Vertices.Min(v => v.Point.Y);
        float maxX = Vertices.Max(v => v.Point.X);
        float maxY = Vertices.Max(v => v.Point.Y);
        
        return (new Vector2(minX, minY), new Vector2(maxX, maxY));
    }
}