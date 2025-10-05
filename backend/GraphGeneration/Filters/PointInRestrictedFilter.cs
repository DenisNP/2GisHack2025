using GraphGeneration.Models;
using NetTopologySuite.Geometries;
using VoronatorSharp;

namespace GraphGeneration.Filters;

public class PointInRestrictedFilter : IPointFilter 
{
    private readonly PolygonMap _polygonMap;

    public PointInRestrictedFilter(PolygonMap polygonMap)
    {
        _polygonMap = polygonMap;
    }
    
    public bool Skip(Vector2 a)
    {
        // Создаем геометрическое представление ребра
        var lineString = new Point(a.x, a.y);

        // Проверяем, пересекает ли ребро любой из игнорируемых полигонов
        foreach (var restricted in _polygonMap.Restricted)
        {
            if (restricted.Contains(lineString))
            {
                // Если ребро пересекает игнорируемый полигон, пропускаем его
                return true;
            }
        }

        return false;
    }
}