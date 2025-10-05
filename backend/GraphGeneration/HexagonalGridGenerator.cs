


using AntAlgorithm;
using VoronatorSharp;

namespace GraphGeneration;

public class HexagonalGridGenerator
{
    
    public static float CalculateExpectedHexDistance(float hexSize)
    {
        // В гексагональной сетке есть два основных расстояния:
        // 1. Расстояние между соседями в одном ряду (horizontalSpacing)
        // 2. Расстояние между соседями в соседних рядах
    
        var horizontalSpacing = hexSize * 2f;
        var verticalSpacing = hexSize * (float)Math.Sqrt(3);
    
        // Для гексагональной сетки среднее расстояние можно аппроксимировать
        // как среднее между основными расстояниями между соседями
    
        // Расстояние до 6 соседей в гексагональной сетке:
        // - 2 соседа на расстоянии horizontalSpacing
        // - 4 соседа на расстоянии sqrt((horizontalSpacing/2)^2 + verticalSpacing^2)
    
        var diagonalDistance = (float)Math.Sqrt(
            Math.Pow(horizontalSpacing * 0.5f, 2) + 
            Math.Pow(verticalSpacing, 2)
        );
    
        // Среднее расстояние до ближайших соседей
        return (horizontalSpacing * 2 + diagonalDistance * 4) / 6f;
    }
    
    public static List<Vector2> GenerateHexagonalGridInPolygon(int maxId, ZonePolygon zonePolygon, float hexSize)
    {
        var points = new List<Vector2>();
        var (min, max) = zonePolygon.GetBoundingBox();
        
        // Расстояния между центрами шестиугольников
        var horizontalSpacing = hexSize * 2f;
        var verticalSpacing = hexSize * (float)Math.Sqrt(3);
        
        // Добавляем отступ для краев
        var padding = hexSize;
        min.X -= padding;
        min.Y -= padding;
        max.X += padding;
        max.Y += padding;
        
        var row = 0;
        
        for (var y = min.Y; y <= max.Y; y += verticalSpacing, row++)
        {
            // Смещение для нечетных рядов
            var xOffset = (row % 2 == 1) ? horizontalSpacing * 0.5f : 0f;
             
            for (var x = min.X + xOffset; x <= max.X; x += horizontalSpacing)
            {
                float RandomNumber() => (float)(Random.Shared.NextDouble() * 0.5f) - 0.25f;
                var point = new Vector2(maxId++, x + RandomNumber(), y + RandomNumber(), 0);
                if (!zonePolygon.ContainsPoint(point) || zonePolygon.Type == ZoneType.Restricted)
                {
                    continue;
                }
                points.Add(point);
            }
        }
        
        return points;
    }
    
    // Альтернативный метод: гексагональная сетка с дополнительными точками на границах
    public static List<Vector2> GenerateDenseHexagonalGrid(int maxId, ZonePolygon zonePolygon, float hexSize, int density = 1)
    {
        var points = new List<Vector2>();
        
        if (density <= 1)
            return GenerateHexagonalGridInPolygon(maxId, zonePolygon, hexSize);
        
        // Генерируем несколько слоев со смещением для лучшего покрытия
        for (var layer = 0; layer < density; layer++)
        {
            var layerOffset = hexSize * layer / density;
            points.AddRange(GenerateHexagonalGridWithOffset(zonePolygon, hexSize, layerOffset));
        }
        
        return points.Distinct().ToList();
    }
    
    private static List<Vector2> GenerateHexagonalGridWithOffset(ZonePolygon zonePolygon, float hexSize, float offset)
    {
        var points = new List<Vector2>();
        var (min, max) = zonePolygon.GetBoundingBox();
        
        var horizontalSpacing = hexSize * 2.5f;
        var verticalSpacing = hexSize * (float)Math.Sqrt(3);
        
        var padding = hexSize * 2f;
        min.X -= padding;
        min.Y -= padding;
        max.X += padding;
        max.Y += padding;
        
        var row = 0;
        
        for (var y = min.Y + offset; y <= max.Y; y += verticalSpacing, row++)
        {
            var xOffset = (row % 2 == 1) ? horizontalSpacing * 0.5f : 0f;
            
            for (var x = min.X + xOffset + offset; x <= max.X; x += horizontalSpacing)
            {
                var point = new Vector2(x, y);
                if (zonePolygon.ContainsPoint(point))
                {
                    points.Add(point);
                }
            }
        }
        
        return points;
    }
}