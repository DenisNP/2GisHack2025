using VoronatorSharp;

namespace VoronatorApp;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
// using VoronatorSharp;

public class PolygonFiller
{
    /// <summary>
    /// Заполняет многоугольник точками с заданной плотностью
    /// </summary>
    /// <param name="polygon">Вершины многоугольника</param>
    /// <param name="pointDensity">Плотность точек (количество точек на единицу площади)</param>
    /// <returns>Массив точек внутри многоугольника</returns>
    public static List<Vector2> FillPolygonWithPoints(List<Vector2> polygon, double pointDensity)
    {
        var points = new List<Vector2>();
        
        // Находим ограничивающий прямоугольник
        var bounds = GetBoundingBox(polygon);
        
        // Генерируем равномерную сетку точек
        double area = bounds.Width * bounds.Height;
        int totalPoints = (int)(area * pointDensity);
        
        // Вычисляем шаг сетки на основе плотности
        double step = 1.0 / Math.Sqrt(pointDensity);
        
        for (double x = bounds.X; x <= bounds.X + bounds.Width; x += step)
        {
            for (double y = bounds.Y; y <= bounds.Y + bounds.Height; y += step)
            {
                var point = new Vector2((float)x, (float)y);
                if (IsPointInPolygon(point, polygon))
                {
                    points.Add(point);
                }
            }
        }
        
        return points;
    }
    
    /// <summary>
    /// Заполняет несколько многоугольников точками и строит триангуляцию Делоне
    /// </summary>
    public static (List<Vector2> points, List<Triangle> triangles) FillMultiplePolygonsWithDelaunay(
        List<List<Vector2>> polygons, 
        double pointDensity)
    {
        var allPoints = new List<Vector2>();
        
        // Собираем все точки из всех многоугольников
        foreach (var polygon in polygons)
        {
            var polygonPoints = FillPolygonWithPoints(polygon, pointDensity);
            allPoints.AddRange(polygonPoints);
        }
        
        // Добавляем вершины многоугольников для сохранения границ
        foreach (var polygon in polygons)
        {
            allPoints.AddRange(polygon);
        }
        
        // Строим триангуляцию Делоне
        var delaunay = new Delaunator(allPoints.ToArray());
        var triangles = GetTriangles(delaunay);
        
        return (allPoints, triangles);
    }
    
    /// <summary>
    /// Проверяет, находится ли точка внутри многоугольника (алгоритм winding number)
    /// </summary>
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
                {
                    windingNumber++;
                }
            }
            else
            {
                if (next.y <= point.y && IsLeft(current, next, point) < 0)
                {
                    windingNumber--;
                }
            }
        }
        
        return windingNumber != 0;
    }
    
    /// <summary>
    /// Определяет положение точки относительно отрезка
    /// </summary>
    private static float IsLeft(Vector2 a, Vector2 b, Vector2 point)
    {
        return (b.x - a.x) * (point.y - a.y) - (point.x - a.x) * (b.y - a.y);
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
    
    /// <summary>
    /// Получает треугольники из триангуляции Делоне
    /// </summary>
    private static List<Triangle> GetTriangles(Delaunator delaunay)
    {
        var triangles = new List<Triangle>();
        
        for (int i = 0; i < delaunay.Triangles.Length; i += 3)
        {
            var triangle = new Triangle(i / 3,
                delaunay.Points[delaunay.Triangles[i]],
                delaunay.Points[delaunay.Triangles[i + 1]],
                delaunay.Points[delaunay.Triangles[i + 2]]
            );
            triangles.Add(triangle);
        }
        
        return triangles;
    }
}


public struct BoundingBox
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    
    public BoundingBox(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}



// Пример использования
class Program
{
    static void Main()
    {
        // Создаем несколько многоугольников
        var polygons = new List<List<Vector2>>
        {
            // Первый многоугольник (прямоугольник)
            new List<Vector2>
            {
                new Vector2(0, 0),
                new Vector2(100, 0),
                new Vector2(100, 50),
                new Vector2(0, 50)
            },
            
            // Второй многоугольник (треугольник)
            new List<Vector2>
            {
                new Vector2(150, 0),
                new Vector2(200, 80),
                new Vector2(100, 80)
            },
            
            // Третий многоугольник (неправильная форма)
            new List<Vector2>
            {
                new Vector2(50, 100),
                new Vector2(80, 120),
                new Vector2(70, 150),
                new Vector2(30, 150),
                new Vector2(20, 120)
            }
        };
        
        // Заполняем многоугольники точками и строим триангуляцию
        double pointDensity = 0.01; // точек на единицу площади
        var result = PolygonFiller.FillMultiplePolygonsWithDelaunay(polygons, pointDensity);
        
        Console.WriteLine($"Сгенерировано точек: {result.points.Count}");
        Console.WriteLine($"Получено треугольников: {result.triangles.Count}");
        
        // Здесь можно визуализировать результат или сохранить данные
        // Например, отрисовать в WPF, WinForms или сохранить в файл
    }
}