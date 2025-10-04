
using VoronatorSharp;

namespace GraphGeneration;

public class HexagonalMultiPolygonGenerator
{
    public class HexagonalSettings
    {
        public float HexSize { get; set; } = 10f;
        public int Density { get; set; } = 1; // 1 = базовая сетка, >1 = уплотненная сетка
        public bool UseConvexHull { get; set; } = true;
        public bool AddPolygonVertices { get; set; } = true;
        public bool AddEdgePoints { get; set; } = false;
        public float EdgePointSpacing { get; set; } = 5f;
    }
    
    public static List<Vector2> GenerateHexagonalPoints(
        List<Polygon> sourcePolygons,
        HexagonalSettings settings = null)
    {
        settings ??= new HexagonalSettings();
        
        var points = new List<Vector2>();
        
        // 1. Находим общий ограничивающий полигон
        Polygon boundingPolygon = settings.UseConvexHull
            ? CalculateConvexHull(GetAllVertices(sourcePolygons))
            : CalculateBoundingPolygon(sourcePolygons);
        
        // 2. Генерируем гексагональную сетку в bounding полигоне
        if (settings.Density > 1)
        {
            points.AddRange(HexagonalGridGenerator.GenerateDenseHexagonalGrid(
                boundingPolygon, settings.HexSize, settings.Density));
        }
        else
        {
            points.AddRange(HexagonalGridGenerator.GenerateHexagonalGridInPolygon(
                boundingPolygon, settings.HexSize));
        }
        
        // 3. Фильтруем точки, оставляя только внутри исходных полигонов
        points = FilterPointsBySourcePolygons(points, sourcePolygons);
        
        // 4. Добавляем вершины полигонов, если нужно
        if (settings.AddPolygonVertices)
        {
            points.AddRange(GetAllVertices(sourcePolygons));
        }
        
        // // 5. Добавляем точки на рёбрах, если нужно
        // if (settings.AddEdgePoints)
        // {
        //     points.AddRange(GenerateHexagonalEdgePoints(sourcePolygons, settings.HexSize, settings.EdgePointSpacing));
        // }
        
        return points.Distinct().ToList();
    }
    
    // Специальные точки на рёбрах для улучшения границ
    // private static List<Vector2> GenerateHexagonalEdgePoints(List<Polygon> sourcePolygons, float hexSize, float edgeSpacing)
    // {
    //     var edgePoints = new List<Vector2>();
    //     
    //     foreach (var polygon in sourcePolygons)
    //     {
    //         for (int i = 0; i < polygon.Vertices.Count; i++)
    //         {
    //             Vector2 start = polygon.Vertices[i].Point;
    //             Vector2 end = polygon.Vertices[(i + 1) % polygon.Vertices.Count].Point;
    //             
    //             // Добавляем точки вдоль ребра с учетом гексагонального шага
    //             Vector2 direction = Vector2.Normalize(end - start);
    //             float edgeLength = Vector2.Distance(start, end);
    //             
    //             // Используем меньший шаг для лучшего соответствия гексагональной сетке
    //             float step = Math.Min(hexSize * 0.5f, edgeSpacing);
    //             int steps = (int)(edgeLength / step);
    //             
    //             for (int j = 1; j < steps; j++)
    //             {
    //                 float t = (float)j / steps;
    //                 Vector2 point = Vector2.Lerp(start, end, t);
    //                 
    //                 // Добавляем небольшие перпендикулярные смещения для лучшего соединения с сеткой
    //                 Vector2 perpendicular = new Vector2(-direction.Y, direction.X);
    //                 
    //                 edgePoints.Add(point);
    //                 edgePoints.Add(point + perpendicular * hexSize * 0.3f);
    //                 edgePoints.Add(point - perpendicular * hexSize * 0.3f);
    //             }
    //         }
    //     }
    //     
    //     return edgePoints;
    // }
    
    // Вспомогательные методы (те же, что и раньше)
    private static Polygon CalculateConvexHull(List<Vector2> points)
    {
        if (points.Count < 3)
            return new Polygon(points);
            
        Vector2 pivot = points.OrderBy(p => p.Y).ThenBy(p => p.X).First();
        
        var sortedPoints = points
            .Where(p => p != pivot)
            .OrderBy(p => Math.Atan2(p.Y - pivot.Y, p.X - pivot.X))
            .ToList();
        
        var hull = new Stack<Vector2>();
        hull.Push(pivot);
        hull.Push(sortedPoints[0]);
        
        for (int i = 1; i < sortedPoints.Count; i++)
        {
            Vector2 top = hull.Pop();
            
            while (hull.Count > 0 && Cross(hull.Peek(), top, sortedPoints[i]) <= 0)
            {
                top = hull.Pop();
            }
            
            hull.Push(top);
            hull.Push(sortedPoints[i]);
        }
        
        return new Polygon(hull.Reverse());
    }
    
    private static Polygon CalculateBoundingPolygon(List<Polygon> polygons)
    {
        if (polygons.Count == 0)
            return new Polygon();
            
        var allVertices = GetAllVertices(polygons);
        
        float minX = allVertices.Min(v => v.X);
        float minY = allVertices.Min(v => v.Y);
        float maxX = allVertices.Max(v => v.X);
        float maxY = allVertices.Max(v => v.Y);
        
        float padding = Math.Min(maxX - minX, maxY - minY) * 0.1f;
        
        return new Polygon(new[]
        {
            new Vector2(minX - padding, minY - padding),
            new Vector2(maxX + padding, minY - padding),
            new Vector2(maxX + padding, maxY + padding),
            new Vector2(minX - padding, maxY + padding)
        });
    }
    
    private static List<Vector2> GetAllVertices(List<Polygon> polygons)
    {
        return polygons.SelectMany(p => p.Vertices).ToList();
    }
    
    private static List<Vector2> FilterPointsBySourcePolygons(List<Vector2> points, List<Polygon> sourcePolygons)
    {
        return points.Where(point => sourcePolygons.Any(polygon => polygon.ContainsPoint(point))).ToList();
    }
    
    private static float Cross(Vector2 o, Vector2 a, Vector2 b)
    {
        return (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
    }
}