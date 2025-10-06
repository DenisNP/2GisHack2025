using NetTopologySuite.Geometries;
using PathScape.Domain.Models;
using VoronatorSharp;

namespace GraphGeneration.Models;

public class ZonePolygon
{
    public List<Vector2> Vertices { get; set; } = [];
    
    public ZonePolygon() { }
    
    public ZoneType Type { get; set; }
    
    public ZonePolygon(Polygon polygon, ZoneType type)
    {
        Vertices = polygon.Coordinates
            .Select((i,v) => new Vector2 { Id = v, x = (float)i.X, y = (float)i.Y, Weight = 0 })
            .ToList();
        Type = type;
    }
    
    public ZonePolygon(IEnumerable<Vector2> vertices, ZoneType type = ZoneType.None)
    {
        Vertices = vertices.Select((i,v) => new Vector2 { Id = v, x = i.X, y = i.y, Weight = 0 }).ToList();
        Type = type;
    }
    
    // Проверка, находится ли точка внутри полигона (алгоритм winding number)
    public bool ContainsPoint(Vector2 point)
    {
        var windingNumber = 0;
        var n = Vertices.Count;
        
        for (var i = 0; i < n; i++)
        {
            var current = Vertices[i];
            var next = Vertices[(i + 1) % n];
            
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
        var minX = Vertices.Min(v => v.X);
        var minY = Vertices.Min(v => v.Y);
        var maxX = Vertices.Max(v => v.X);
        var maxY = Vertices.Max(v => v.Y);
        
        return (new Vector2(minX, minY), new Vector2(maxX, maxY));
    }
}