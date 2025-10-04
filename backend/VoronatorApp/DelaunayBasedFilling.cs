using System;
using System.Collections.Generic;
using System.Linq;
using VoronatorSharp;
using VoronatorSharp;

namespace VoronatorApp;
using System;
using System.Collections.Generic;
using System.Linq;
using VoronatorSharp;

public partial class DelaunayBasedFilling
{
    /// <summary>
    /// Заполняет несколько многоугольников точками и строит триангуляцию Делоне
    /// Точки добавляются ТОЛЬКО внутри полигонов
    /// </summary>
    // public static (List<Vector2> points, List<Triangle> triangles) FillMultiplePolygonsWithDelaunay(
    //     List<List<Vector2>> polygons,
    //     double pointDensity)
    // {
    //     var allPoints = new List<Vector2>();
    //
    //     // Собираем ВСЕ точки из ВСЕХ многоугольников (только внутри)
    //     foreach (var polygon in polygons)
    //     {
    //         var polygonPoints = FillPolygonWithPoints(polygon, pointDensity);
    //         allPoints.AddRange(polygonPoints);
    //     
    //         // НЕ добавляем вершины полигонов - только сгенерированные точки внутри
    //         // allPoints.AddRange(polygon); // ← закомментировано!
    //     }
    //
    //     // Фильтруем точки - оставляем только те, что внутри хотя бы одного полигона
    //     var filteredPoints = allPoints.Where(p => IsPointInAnyPolygon(p, polygons)).ToList();
    //
    //     // Если точек слишком мало, добавляем несколько вершин для стабильности триангуляции
    //     if (filteredPoints.Count < 3)
    //     {
    //         foreach (var polygon in polygons)
    //         {
    //             filteredPoints.AddRange(polygon.Take(3)); // добавляем первые 3 вершины каждого полигона
    //         }
    //     }
    //
    //     // Строим триангуляцию Делоне
    //     var delaunay = new Delaunator(filteredPoints.ToArray());
    //     var triangles = GetTriangles(delaunay);
    //
    //     // Фильтруем треугольники - оставляем только те, чьи центроиды внутри полигонов
    //     var filteredTriangles = triangles.Where(t => 
    //     {
    //         var centroid = GetTriangleCentroid(t);
    //         return IsPointInAnyPolygon(centroid, polygons);
    //     }).ToList();
    //
    //     return (filteredPoints, filteredTriangles);
    // }
    
    /// <summary>
    /// Генерирует точки внутри полигонов на основе центроидов треугольников Делоне
    /// </summary>
    public static List<Vector2> GeneratePointsFromDelaunay(List<List<Vector2>> polygons, int refinementIterations = 3)
    {
        var allPoints = new List<Vector2>();
        
        // Собираем все вершины полигонов
        foreach (var polygon in polygons)
        {
            allPoints.AddRange(polygon);
        }
        
        // Добавляем дополнительные точки для начальной триангуляции
        AddBoundingPoints(allPoints);
        
        for (int iteration = 0; iteration < refinementIterations; iteration++)
        {
            // Строим триангуляцию Делоне
            var delaunay = new Delaunator(allPoints.ToArray());
            var newPoints = new List<Vector2>();
            
            // Получаем все треугольники
            var triangles = GetTriangles(delaunay);
            
            // Добавляем центроиды треугольников, которые находятся внутри полигонов
            foreach (var triangle in triangles)
            {
                var centroid = GetTriangleCentroid(triangle);
                
                // Проверяем, находится ли центроид внутри любого из полигонов
                if (IsPointInAnyPolygon(centroid, polygons))
                {
                    newPoints.Add(centroid);
                }
            }
            
            // Добавляем только уникальные точки
            foreach (var point in newPoints)
            {
                if (!allPoints.Any(p => Distance(p, point) < 1.0f))
                {
                    allPoints.Add(point);
                }
            }
            
            Console.WriteLine($"Итерация {iteration + 1}: добавлено {newPoints.Count} точек");
        }
        
        return allPoints.Where(p => IsPointInAnyPolygon(p, polygons)).ToList();
    }
    
    /// <summary>
    /// Генерация точек через рекурсивное разбиение ребер
    /// </summary>
    public static List<Vector2> GeneratePointsByEdgeRefinement(List<List<Vector2>> polygons, int maxDepth = 4)
    {
        var allPoints = new List<Vector2>();
        
        // Начинаем с вершин полигонов
        foreach (var polygon in polygons)
        {
            allPoints.AddRange(polygon);
        }
        
        // Рекурсивно разбиваем ребра
        for (int depth = 0; depth < maxDepth; depth++)
        {
            var newPoints = new List<Vector2>();
            var delaunay = new Delaunator(allPoints.ToArray());
            
            // Получаем все ребра триангуляции
            var edges = GetUniqueEdges(delaunay);
            
            foreach (var edge in edges)
            {
                var midpoint = new Vector2(
                    (edge.Start.x + edge.End.x) / 2,
                    (edge.Start.y + edge.End.y) / 2
                );
                
                // Добавляем среднюю точку, если она внутри полигона
                if (IsPointInAnyPolygon(midpoint, polygons) && 
                    !allPoints.Any(p => Distance(p, midpoint) < 0.1f))
                {
                    newPoints.Add(midpoint);
                }
            }
            
            allPoints.AddRange(newPoints);
            Console.WriteLine($"Глубина {depth + 1}: добавлено {newPoints.Count} точек");
        }
        
        return allPoints;
    }
    
    /// <summary>
    /// Адаптивная генерация на основе площади треугольников
    /// </summary>
    public static List<Vector2> GenerateAdaptivePoints(List<List<Vector2>> polygons, float maxTriangleArea = 100f)
    {
        var allPoints = new List<Vector2>();
        
        // Начинаем с вершин полигонов
        foreach (var polygon in polygons)
        {
            allPoints.AddRange(polygon);
        }
        
        bool addedPoints;
        do
        {
            addedPoints = false;
            var delaunay = new Delaunator(allPoints.ToArray());
            var triangles = GetTriangles(delaunay);
            var newPoints = new List<Vector2>();
            
            foreach (var triangle in triangles)
            {
                var area = GetTriangleArea(triangle);
                var centroid = GetTriangleCentroid(triangle);
                
                // Если треугольник слишком большой и его центроид внутри полигона, добавляем точку
                if (area > maxTriangleArea && IsPointInAnyPolygon(centroid, polygons))
                {
                    if (!allPoints.Any(p => Distance(p, centroid) < 1.0f))
                    {
                        newPoints.Add(centroid);
                        addedPoints = true;
                    }
                }
            }
            
            allPoints.AddRange(newPoints);
            Console.WriteLine($"Добавлено {newPoints.Count} точек, общее количество: {allPoints.Count}");
            
        } while (addedPoints);
        
        return allPoints.Where(p => IsPointInAnyPolygon(p, polygons)).ToList();
    }
    
    /// <summary>
    /// Комбинированный подход - центроиды + случайные точки в больших треугольниках
    /// </summary>
    public static List<Vector2> GenerateMixedPoints(List<List<Vector2>> polygons, int iterations = 5)
    {
        var allPoints = new List<Vector2>();
        var random = new Random();
        
        foreach (var polygon in polygons)
        {
            allPoints.AddRange(polygon);
        }
        
        for (int iter = 0; iter < iterations; iter++)
        {
            var delaunay = new Delaunator(allPoints);
            var triangles = GetTriangles(delaunay);
            var newPoints = new List<Vector2>();
            
            foreach (var triangle in triangles)
            {
                var centroid = GetTriangleCentroid(triangle);
                
                if (IsPointInAnyPolygon(centroid, polygons))
                {
                    // Всегда добавляем центроид
                    if (!allPoints.Any(p => Distance(p, centroid) < 1.0f))
                    {
                        newPoints.Add(centroid);
                    }
                    
                    // Для больших треугольников добавляем случайные точки
                    var area = GetTriangleArea(triangle);
                    if (area > 50f)
                    {
                        for (int j = 0; j < 2; j++)
                        {
                            var randomPoint = GetRandomPointInTriangle(triangle, random);
                            if (!allPoints.Any(p => Distance(p, randomPoint) < 1.0f) &&
                                !newPoints.Any(p => Distance(p, randomPoint) < 1.0f))
                            {
                                newPoints.Add(randomPoint);
                            }
                        }
                    }
                }
            }
            
            allPoints.AddRange(newPoints);
            Console.WriteLine($"Итерация {iter + 1}: добавлено {newPoints.Count} точек");
        }
        
        return allPoints.Where(p => IsPointInAnyPolygon(p, polygons)).ToList();
    }
    
    /// <summary>
    /// Получает все треугольники из триангуляции Делоне
    /// </summary>
    public static List<Triangle> GetTriangles(Delaunator delaunay)
    {
        var triangles = new List<Triangle>();
        
        // В Delaunator треугольники хранятся в массиве Triangles как индексы точек
        for (int i = 0; i < delaunay.Triangles.Length; i += 3)
        {
            var point1 = delaunay.Points[delaunay.Triangles[i]];
            var point2 = delaunay.Points[delaunay.Triangles[i + 1]];
            var point3 = delaunay.Points[delaunay.Triangles[i + 2]];
            
            var triangle = new Triangle(i/3,point1, point2, point3);
            triangles.Add(triangle);
        }
        
        return triangles;
    }
    
    /// <summary>
    /// Получает уникальные ребра из триангуляции Делоне
    /// </summary>
    private static List<Edge> GetUniqueEdges(Delaunator delaunay)
    {
        var edges = new HashSet<Edge>();
        var triangles = GetTriangles(delaunay);
        
        foreach (var triangle in triangles)
        {
            edges.Add(new Edge(triangle.Point1, triangle.Point2));
            edges.Add(new Edge(triangle.Point2, triangle.Point3));
            edges.Add(new Edge(triangle.Point3, triangle.Point1));
        }
        
        return edges.ToList();
    }
    
    // Остальные вспомогательные методы остаются без изменений
    public static Vector2 GetTriangleCentroid(Triangle triangle)
    {
        return new Vector2(
            (triangle.Point1.x + triangle.Point2.x + triangle.Point3.x) / 3,
            (triangle.Point1.y + triangle.Point2.y + triangle.Point3.y) / 3
        );
    }
    
    private static float GetTriangleArea(Triangle triangle)
    {
        return Math.Abs(
            (triangle.Point1.x * (triangle.Point2.y - triangle.Point3.y) +
             triangle.Point2.x * (triangle.Point3.y - triangle.Point1.y) +
             triangle.Point3.x * (triangle.Point1.y - triangle.Point2.y)) / 2
        );
    }
    
    private static Vector2 GetRandomPointInTriangle(Triangle triangle, Random random)
    {
        float r1 = (float)random.NextDouble();
        float r2 = (float)random.NextDouble();
        
        if (r1 + r2 > 1)
        {
            r1 = 1 - r1;
            r2 = 1 - r2;
        }
        
        float r3 = 1 - r1 - r2;
        
        return new Vector2(
            r1 * triangle.Point1.x + r2 * triangle.Point2.x + r3 * triangle.Point3.x,
            r1 * triangle.Point1.y + r2 * triangle.Point2.y + r3 * triangle.Point3.y
        );
    }
    
    private static void AddBoundingPoints(List<Vector2> points)
    {
        if (points.Count == 0) return;
        
        float minX = points.Min(p => p.x);
        float maxX = points.Max(p => p.x);
        float minY = points.Min(p => p.y);
        float maxY = points.Max(p => p.y);
        
        float width = maxX - minX;
        float height = maxY - minY;
        
        // Добавляем точки по границам
        points.Add(new Vector2(minX - width * 0.1f, minY - height * 0.1f));
        points.Add(new Vector2(maxX + width * 0.1f, minY - height * 0.1f));
        points.Add(new Vector2(minX - width * 0.1f, maxY + height * 0.1f));
        points.Add(new Vector2(maxX + width * 0.1f, maxY + height * 0.1f));
    }
    
    public static bool IsPointInAnyPolygon(Vector2 point, List<List<Vector2>> polygons)
    {
        return polygons.Any(polygon => IsPointInPolygon(point, polygon));
    }
    
    private static bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        int windingNumber = 0;
        
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 current = polygon[i];
            Vector2 next = polygon[(i + 1) % polygon.Count];
            
            if (current.y <= point.y)
            {
                if (next.y > point.y && IsLeft(current, next, point) > 0)
                    windingNumber++;
            }
            else
            {
                if (next.y <= point.y && IsLeft(current, next, point) < 0)
                    windingNumber--;
            }
        }
        
        return windingNumber != 0;
    }
    
    private static float IsLeft(Vector2 a, Vector2 b, Vector2 point)
    {
        return (b.x - a.x) * (point.y - a.y) - (point.x - a.x) * (b.y - a.y);
    }
    
    private static float Distance(Vector2 a, Vector2 b)
    {
        float dx = a.x - b.x;
        float dy = a.y - b.y;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }
}

// Структуры данных

public struct Edge
{
    public Vector2 Start { get; set; }
    public Vector2 End { get; set; }
    
    public Edge(Vector2 start, Vector2 end)
    {
        Start = start;
        End = end;
    }
    
    public override bool Equals(object obj)
    {
        if (obj is Edge other)
        {
            return (Start.Equals(other.Start) && End.Equals(other.End)) ||
                   (Start.Equals(other.End) && End.Equals(other.Start));
        }
        return false;
    }
    
    public override int GetHashCode()
    {
        return Start.GetHashCode() ^ End.GetHashCode();
    }
}

