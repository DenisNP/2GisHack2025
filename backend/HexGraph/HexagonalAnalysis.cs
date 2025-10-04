using System.Numerics;

namespace HexGraph;

public class HexagonalAnalysis
{
    // Анализ качества гексагональной сетки
    public static void AnalyzeGrid(List<Vector2> points, float expectedHexSize)
    {
        if (points.Count < 2)
        {
            Console.WriteLine("Недостаточно точек для анализа");
            return;
        }
        
        // Анализируем расстояния между соседними точками
        var distances = new List<float>();
        
        for (int i = 0; i < points.Count; i++)
        {
            for (int j = i + 1; j < points.Count; j++)
            {
                float distance = Vector2.Distance(points[i], points[j]);
                if (distance <= expectedHexSize * 1.8f) // Близкие точки
                {
                    distances.Add(distance);
                }
            }
        }
        
        if (distances.Count > 0)
        {
            float avgDistance = distances.Average();
            float minDistance = distances.Min();
            float maxDistance = distances.Max();
            
            Console.WriteLine($"Анализ гексагональной сетки:");
            Console.WriteLine($"  Среднее расстояние: {avgDistance:F2}");
            Console.WriteLine($"  Минимальное расстояние: {minDistance:F2}");
            Console.WriteLine($"  Максимальное расстояние: {maxDistance:F2}");
            Console.WriteLine($"  Ожидаемое расстояние: {expectedHexSize:F2}");
            Console.WriteLine($"  Отклонение: {Math.Abs(avgDistance - expectedHexSize):F2}");
        }
    }
    
    // Оптимизация размера шестиугольника на основе площади полигонов
    public static float CalculateOptimalHexSize(List<Polygon> polygons, int targetPointCount)
    {
        float totalArea = 0f;
        foreach (var polygon in polygons)
        {
            totalArea += CalculatePolygonArea(polygon);
        }
        
        // Площадь одного шестиугольника
        float hexArea = (3f * (float)Math.Sqrt(3) * 1f * 1f) / 2f; // Для размера 1
        
        float requiredHexArea = totalArea / targetPointCount;
        float optimalSize = (float)Math.Sqrt(requiredHexArea / hexArea);
        
        return optimalSize;
    }
    
    private static float CalculatePolygonArea(Polygon polygon)
    {
        float area = 0f;
        int n = polygon.Vertices.Count;
        
        for (int i = 0; i < n; i++)
        {
            Vector2 current = polygon.Vertices[i];
            Vector2 next = polygon.Vertices[(i + 1) % n];
            area += current.X * next.Y - next.X * current.Y;
        }
        
        return Math.Abs(area) / 2f;
    }
}