

using System.Numerics;

namespace HexGraph;

public class HexagonalGridGenerator
{
    
    public static float CalculateExpectedHexDistance(float hexSize)
    {
        // В гексагональной сетке есть два основных расстояния:
        // 1. Расстояние между соседями в одном ряду (horizontalSpacing)
        // 2. Расстояние между соседями в соседних рядах
    
        float horizontalSpacing = hexSize * 2f;
        float verticalSpacing = hexSize * (float)Math.Sqrt(3);
    
        // Для гексагональной сетки среднее расстояние можно аппроксимировать
        // как среднее между основными расстояниями между соседями
    
        // Расстояние до 6 соседей в гексагональной сетке:
        // - 2 соседа на расстоянии horizontalSpacing
        // - 4 соседа на расстоянии sqrt((horizontalSpacing/2)^2 + verticalSpacing^2)
    
        float diagonalDistance = (float)Math.Sqrt(
            Math.Pow(horizontalSpacing * 0.5f, 2) + 
            Math.Pow(verticalSpacing, 2)
        );
    
        // Среднее расстояние до ближайших соседей
        return (horizontalSpacing * 2 + diagonalDistance * 4) / 6f;
    }
    
    public static List<Vector2> GenerateHexagonalGridInPolygon(Polygon polygon, float hexSize)
    {
        var points = new List<Vector2>();
        var (min, max) = polygon.GetBoundingBox();
        
        // Расстояния между центрами шестиугольников
        float horizontalSpacing = hexSize * 2f;
        float verticalSpacing = hexSize * (float)Math.Sqrt(3);
        
        // Добавляем отступ для краев
        float padding = hexSize;
        min.X -= padding;
        min.Y -= padding;
        max.X += padding;
        max.Y += padding;
        
        int row = 0;
        
        for (float y = min.Y; y <= max.Y; y += verticalSpacing, row++)
        {
            // Смещение для нечетных рядов
            float xOffset = (row % 2 == 1) ? horizontalSpacing * 0.5f : 0f;
             
            for (float x = min.X + xOffset; x <= max.X; x += horizontalSpacing)
            {
                var randomNumber = () => (float)(Random.Shared.NextDouble() * 0.3f) - 0.15f;
                var point = new Vector2(x + randomNumber(), y + randomNumber());
                if (polygon.ContainsPoint(point))
                {
                    points.Add(point);
                }
            }
        }
        
        return points;
    }
    
    // Альтернативный метод: гексагональная сетка с дополнительными точками на границах
    public static List<Vector2> GenerateDenseHexagonalGrid(Polygon polygon, float hexSize, int density = 1)
    {
        var points = new List<Vector2>();
        
        if (density <= 1)
            return GenerateHexagonalGridInPolygon(polygon, hexSize);
        
        // Генерируем несколько слоев со смещением для лучшего покрытия
        for (int layer = 0; layer < density; layer++)
        {
            float layerOffset = hexSize * layer / density;
            points.AddRange(GenerateHexagonalGridWithOffset(polygon, hexSize, layerOffset));
        }
        
        return points.Distinct().ToList();
    }
    
    private static List<Vector2> GenerateHexagonalGridWithOffset(Polygon polygon, float hexSize, float offset)
    {
        var points = new List<Vector2>();
        var (min, max) = polygon.GetBoundingBox();
        
        float horizontalSpacing = hexSize * 2.5f;
        float verticalSpacing = hexSize * (float)Math.Sqrt(3);
        
        float padding = hexSize * 2f;
        min.X -= padding;
        min.Y -= padding;
        max.X += padding;
        max.Y += padding;
        
        int row = 0;
        
        for (float y = min.Y + offset; y <= max.Y; y += verticalSpacing, row++)
        {
            float xOffset = (row % 2 == 1) ? horizontalSpacing * 0.5f : 0f;
            
            for (float x = min.X + xOffset + offset; x <= max.X; x += horizontalSpacing)
            {
                var point = new Vector2(x, y);
                if (polygon.ContainsPoint(point))
                {
                    points.Add(point);
                }
            }
        }
        
        return points;
    }
}