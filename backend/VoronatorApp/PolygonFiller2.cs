using VoronatorSharp;

namespace VoronatorApp;

public partial class DelaunayBasedFilling
{
    /// <summary>
    /// Заполняет полигоны точками с гарантией, что все точки будут строго внутри
    /// </summary>
    public static (List<Vector2> points, List<Triangle> triangles) FillPolygonsStrictlyInside(
        List<List<Vector2>> polygons,
        int pointsPerPolygon = 50)
    {
        var allPoints = new List<Vector2>();
        var random = new Random();
    
        foreach (var polygon in polygons)
        {
            var bounds = GetBoundingBox(polygon);
        
            for (int i = 0; i < pointsPerPolygon; i++)
            {
                Vector2 point;
                int attempts = 0;
            
                // Генерируем точки до тех пор, пока не получим точку внутри полигона
                do
                {
                    point = new Vector2(
                        bounds.X + (float)random.NextDouble() * bounds.Width,
                        bounds.Y + (float)random.NextDouble() * bounds.Height
                    );
                    attempts++;
                
                    // Защита от бесконечного цикла
                    if (attempts > 1000) break;
                
                } while (!IsPointInPolygon(point, polygon));
            
                if (attempts <= 1000) // Если нашли валидную точку
                {
                    allPoints.Add(point);
                }
            }
        }
    
        // Строим триангуляцию
        var delaunay = new Delaunator(allPoints.ToArray());
        var allTriangles = GetTriangles(delaunay);
    
        // Фильтруем треугольники (только те, что полностью внутри полигонов)
        var validTriangles = allTriangles.Where(t => 
            IsTriangleInsideAnyPolygon(t, polygons)
        ).ToList();
    
        return (allPoints, validTriangles);
    }

    /// <summary>
    /// Проверяет, находится ли треугольник полностью внутри любого из полигонов
    /// </summary>
    public static bool IsTriangleInsideAnyPolygon(Triangle triangle, List<List<Vector2>> polygons)
    {
        var centroid = GetTriangleCentroid(triangle);
        return IsPointInAnyPolygon(centroid, polygons);
    }
    
    /// <summary>
    /// Находит ограничивающий прямоугольник для многоугольника
    /// </summary>
    private static BoundingBox GetBoundingBox(List<Vector2> polygon)
    {
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        
        foreach (var point in polygon)
        {
            minX = Math.Min(minX, point.x);
            minY = Math.Min(minY, point.y);
            maxX = Math.Max(maxX, point.x);
            maxY = Math.Max(maxY, point.y);
        }
        
        return new BoundingBox(minX, minY, maxX - minX, maxY - minY);
    }
}