using VoronatorSharp;

namespace GraphGeneration;

public class PolygonGenerator
{
    private Random _random;
    
    public PolygonGenerator(int seed = 0)
    {
        _random = seed == 0 ? new Random() : new Random(seed);
    }
    
    public (List<ZonePolygon> polygons, List<Vector2> pois) GeneratePolygonsWithPois(int polygonCount, int poisPerPolygon)
    {
        var polygons = new List<ZonePolygon>();
        var allPois = new List<Vector2>();
        
        // Генерируем полигоны
        for (var i = 0; i < polygonCount; i++)
        {
            var polygon = GenerateRandomPolygon();
            polygons.Add(polygon);
            
            // Генерируем POIs внутри этого полигона
            var polygonPois = GeneratePoisInsidePolygon(polygon, poisPerPolygon, 10000 + i * 100);
            allPois.AddRange(polygonPois);
        }
        
        return (polygons, allPois);
    }
    
    private ZonePolygon GenerateRandomPolygon()
    {
        // Создаем базовый прямоугольник
        float width = _random.Next(20, 50);
        float height = _random.Next(20, 50);
        float startX = _random.Next(0, 100);
        float startY = _random.Next(0, 100);
        
        // Создаем вариации формы (может быть прямоугольник или трапеция)
        var vertices = new List<Vector2>();
        
        if (_random.NextDouble() > 0.5)
        {
            // Прямоугольник
            vertices.Add(new Vector2(startX, startY));
            vertices.Add(new Vector2(startX + width, startY));
            vertices.Add(new Vector2(startX + width, startY + height));
            vertices.Add(new Vector2(startX, startY + height));
            vertices.Add(new Vector2(startX, startY)); // Замыкаем полигон
        }
        else
        {
            // Трапеция
            var offset = width * 0.2f;
            vertices.Add(new Vector2(startX + offset, startY));
            vertices.Add(new Vector2(startX + width - offset, startY));
            vertices.Add(new Vector2(startX + width, startY + height));
            vertices.Add(new Vector2(startX, startY + height));
            vertices.Add(new Vector2(startX + offset, startY)); // Замыкаем полигон
        }
        
        return new ZonePolygon(vertices);
    }
    
    private List<Vector2> GeneratePoisInsidePolygon(ZonePolygon zonePolygon, int count, int startId)
    {
        var pois = new List<Vector2>();
        var bounds = GetPolygonBounds(zonePolygon);
        
        for (var i = 0; i < count; i++)
        {
            Vector2 point;
            var attempts = 0;
            
            // Пытаемся найти точку внутри полигона
            do
            {
                point = new Vector2(
                    (float)(bounds.MinX + _random.NextDouble() * (bounds.MaxX - bounds.MinX)),
                    (float)(bounds.MinY + _random.NextDouble() * (bounds.MaxY - bounds.MinY))
                );
                attempts++;
                
                // Если долго не получается, используем центр тяжести
                if (attempts > 100)
                {
                    point = GetPolygonCentroid(zonePolygon);
                    break;
                }
            }
            while (!IsPointInPolygon(point, zonePolygon));
            
            // Генерируем случайный вес от 0.1 до 2.0
            var weight = (float)(0.1 + _random.NextDouble() * 1.9);
            var poi = new Vector2(startId + i, point.X, point.Y, weight);
            pois.Add(poi);
        }
        
        return pois;
    }
    
    private (float MinX, float MinY, float MaxX, float MaxY) GetPolygonBounds(ZonePolygon zonePolygon)
    {
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        
        foreach (var vertex in zonePolygon.Vertices)
        {
            minX = Math.Min(minX, vertex.X);
            minY = Math.Min(minY, vertex.Y);
            maxX = Math.Max(maxX, vertex.X);
            maxY = Math.Max(maxY, vertex.Y);
        }
        
        return (minX, minY, maxX, maxY);
    }
    
    private Vector2 GetPolygonCentroid(ZonePolygon zonePolygon)
    {
        float area = 0;
        float centroidX = 0;
        float centroidY = 0;
        var vertices = zonePolygon.Vertices.ToArray();
        
        for (var i = 0; i < vertices.Length - 1; i++)
        {
            var cross = vertices[i].X * vertices[i + 1].Y - vertices[i + 1].X * vertices[i].Y;
            area += cross;
            centroidX += (vertices[i].X + vertices[i + 1].X) * cross;
            centroidY += (vertices[i].Y + vertices[i + 1].Y) * cross;
        }
        
        area *= 0.5f;
        centroidX /= (6 * area);
        centroidY /= (6 * area);
        
        return new Vector2(centroidX, centroidY);
    }
    
    // Алгоритм проверки нахождения точки в полигоне
    private bool IsPointInPolygon(Vector2 point, ZonePolygon zonePolygon)
    {
        var inside = false;
        var vertices = zonePolygon.Vertices.ToArray();
        
        for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i++)
        {
            if (((vertices[i].Y > point.Y) != (vertices[j].Y > point.Y)) &&
                (point.X < (vertices[j].X - vertices[i].X) * (point.Y - vertices[i].Y) / 
                          (vertices[j].Y - vertices[i].Y) + vertices[i].X))
            {
                inside = !inside;
            }
        }
        
        return inside;
    }
}
