using GraphGeneration.Filters;
using GraphGeneration.Models;
using VoronatorSharp;

namespace GraphGeneration.Geometry;

public static class HexagonalMultiPolygonGenerator
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
        int maxId,
        PolygonMap polygonMap,
        HexagonalSettings settings)
    {
        var points = new List<Vector2>();
        var pointFilter = new PointRestrictedAndNotUrbanFilter(polygonMap);
        
        // 1. Находим общий ограничивающий полигон
        var boundingPolygon = settings.UseConvexHull
            ? CalculateConvexHull(GetAllVertices(polygonMap.Generation))
            : CalculateBoundingPolygon(polygonMap.Generation);
        
        // 2. Генерируем гексагональную сетку в bounding полигоне
        if (settings.Density > 1)
        {
            points.AddRange(HexagonalGridGenerator.GenerateDenseHexagonalGrid(maxId,
                boundingPolygon, settings.HexSize, settings.Density));
        }
        else
        {
            points.AddRange(HexagonalGridGenerator.GenerateHexagonalGridInPolygon(maxId,
                boundingPolygon, settings.HexSize));
        }
        
        // 3. Фильтруем точки, оставляя только внутри исходных полигонов
        points = FilterPointsBySourcePolygons(pointFilter, points, polygonMap.Generation);
        
        // 4. Добавляем вершины полигонов, если нужно
        if (settings.AddPolygonVertices)
        {
            points.AddRange(GetAllVertices(polygonMap.Generation));
        }
        
        // 5. Добавляем точки на рёбрах, если нужно
        if (settings.AddEdgePoints)
        {
            points.AddRange(GenerateHexagonalEdgePoints(polygonMap, settings.HexSize, settings.EdgePointSpacing));
        }
        
        return points.Distinct().ToList();
    }

    public static List<Vector2> GenerateSpacedHexagonalPointsOutside(int maxId, PolygonMap polygonMap, float hexSize)
    {
        var points = new List<Vector2>();
        var pointFilter = new PointRestrictedUrbanOrAvailableFilter(polygonMap);

        // 1. Находим общий ограничивающий полигон
        var boundingPolygon = CalculateConvexHull(polygonMap.Render.SelectMany(v => v.Coordinates).Select(c => new Vector2((float)c.X, (float)c.Y)).ToList());

        // 2. Генерируем гексагональную сетку в bounding полигоне
        points.AddRange(HexagonalGridGenerator.GenerateHexagonalGridInPolygon(maxId, boundingPolygon, hexSize));
        
        // 3. Фильтруем точки, оставляя только внутри исходных полигонов
        points.RemoveAll(p => pointFilter.Skip(p));
        return points.Distinct().ToList();
    }
    
    // Специальные точки на рёбрах для улучшения границ
    private static List<Vector2> GenerateHexagonalEdgePoints(PolygonMap polygonMap, float hexSize, float edgeSpacing)
    {
        var edgePoints = new List<Vector2>();
        
        foreach (var polygon in polygonMap.Zones)
        {
            for (int i = 0; i < polygon.Vertices.Count; i++)
            {
                Vector2 start = polygon.Vertices[i];
                Vector2 end = polygon.Vertices[(i + 1) % polygon.Vertices.Count];
                
                // Добавляем точки вдоль ребра с учетом гексагонального шага
                Vector2 direction = end - start;
                direction.Normalize();
                float edgeLength = Vector2.Distance(start, end);
                
                // Используем меньший шаг для лучшего соответствия гексагональной сетке
                float step = Math.Min(hexSize * 0.5f, edgeSpacing);
                int steps = (int)(edgeLength / step);
                
                for (int j = 1; j < steps; j++)
                {
                    float t = (float)j / steps;
                    Vector2 point = Vector2.Lerp(start, end, t);
                    
                    // Добавляем небольшие перпендикулярные смещения для лучшего соединения с сеткой
                    Vector2 perpendicular = new Vector2(-direction.Y, direction.X);
                    
                    edgePoints.Add(point);
                    edgePoints.Add(point + perpendicular * hexSize * 0.3f);
                    edgePoints.Add(point - perpendicular * hexSize * 0.3f);
                }
            }
        }
        
        return edgePoints;
    }
    
    // Вспомогательные методы (те же, что и раньше)
    private static ZonePolygon CalculateConvexHull(List<Vector2> points)
    {
        if (points.Count < 3)
            return new ZonePolygon(points);
            
        var pivot = points.OrderBy(p => p.Y).ThenBy(p => p.X).First();
        
        var sortedPoints = points
            .Where(p => p != pivot)
            .OrderBy(p => Math.Atan2(p.Y - pivot.Y, p.X - pivot.X))
            .ToList();
        
        var hull = new Stack<Vector2>();
        hull.Push(pivot);
        hull.Push(sortedPoints[0]);
        
        for (var i = 1; i < sortedPoints.Count; i++)
        {
            var top = hull.Pop();
            
            while (hull.Count > 0 && Cross(hull.Peek(), top, sortedPoints[i]) <= 0)
            {
                top = hull.Pop();
            }
            
            hull.Push(top);
            hull.Push(sortedPoints[i]);
        }
        
        return new ZonePolygon(hull.Reverse());
    }
    
    private static ZonePolygon CalculateBoundingPolygon(IReadOnlyCollection<ZonePolygon> polygons)
    {
        if (polygons.Count == 0)
            return new ZonePolygon();
            
        var allVertices = GetAllVertices(polygons);
        
        var minX = allVertices.Min(v => v.X);
        var minY = allVertices.Min(v => v.Y);
        var maxX = allVertices.Max(v => v.X);
        var maxY = allVertices.Max(v => v.Y);
        
        var padding = Math.Min(maxX - minX, maxY - minY) * 0.1f;
        
        return new ZonePolygon([
            new Vector2(minX - padding, minY - padding),
            new Vector2(maxX + padding, minY - padding),
            new Vector2(maxX + padding, maxY + padding),
            new Vector2(minX - padding, maxY + padding)
        ]);
    }
    
    private static List<Vector2> GetAllVertices(IReadOnlyCollection<ZonePolygon> polygons)
    {
        return polygons.SelectMany(p => p.Vertices).ToList();
    }
    
    private static List<Vector2> FilterPointsBySourcePolygons(IPointFilter pointFilter, List<Vector2> points, IReadOnlyCollection<ZonePolygon> sourcePolygons)
    {
        return points
            .Where(point =>  //(!pointFilter.Skip(point) || point.IsPoi) && 
                            sourcePolygons.Any(polygon => polygon.ContainsPoint(point))
            ).ToList();
    }
    
    private static float Cross(Vector2 o, Vector2 a, Vector2 b)
    {
        return (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
    }
}