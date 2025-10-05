using AntAlgorithm;
using VoronatorSharp;
using Point = NetTopologySuite.Geometries.Point;
using NetTopologySuite.Geometries;

namespace GraphGeneration;

public static class GenerateEdgesGraphDeprecate
{
    public static Edge[] GetEdges(
        List<NetTopologySuite.Geometries.Polygon> restricts,
        Dictionary<NetTopologySuite.Geometries.Polygon, List<Point>> pointsByPolygon,
        Delaunator delaunator,
        float hexSize)
    {
        var result = new List<Edge>();
        var triangles = delaunator.GetTriangles();
        var sr = HexagonalGridGenerator.CalculateExpectedHexDistance(hexSize);

        foreach (var triangle in triangles)
        {
            for (var i = 0; i < 3; i++)
            {
                var tPoints = triangle.ToList();
                var t1 = tPoints[i];
                var t2 = tPoints[(i + 1) % 3];

                if (sr * 1.2 < Vector2.Distance(t1, t2))
                {
                    continue;
                }

                // Создаем геометрическое представление ребра
                var lineString = new LineString([
                    new Coordinate(t1.x, t1.y),
                    new Coordinate(t2.x, t2.y)
                ]);

                // Проверяем, пересекает ли ребро любой из игнорируемых полигонов
                var intersectsIgnoredPolygon = false;
                foreach (var polygon in restricts)
                {
                    if (lineString.Crosses(polygon) || polygon.Contains(lineString))
                    {
                        intersectsIgnoredPolygon = true;
                        break;
                    }
                }

                // Если ребро пересекает игнорируемый полигон, пропускаем его
                if (intersectsIgnoredPolygon)
                {
                    continue;
                }

                // Определяем, является ли ребро межполигональным
                var polygon1 = GetPointPolygon(new Point(t1.x, t1.y), pointsByPolygon);
                var polygon2 = GetPointPolygon(new Point(t2.x, t2.y), pointsByPolygon);

                if (polygon1 == polygon2)
                    result.Add(new Edge(new Poi(t1.Id, t1.X, t1.Y, t1.Weight), new Poi(t2.Id, t2.X, t2.Y, t2.Weight)));
            }
        }

        return result.ToArray();
    }

    // Вспомогательные функции
    private static NetTopologySuite.Geometries.Polygon? GetPointPolygon(Point point, Dictionary<NetTopologySuite.Geometries.Polygon, List<Point>> pointsByPolygon)
    {
        foreach (var kvp in pointsByPolygon)
        {
            if (kvp.Value.Contains(point))
                return kvp.Key;
        }
        return null;
    }
}