using AntAlgorithm;
using VoronatorSharp;
using Point = NetTopologySuite.Geometries.Point;

namespace GraphGeneration;

public static class GenerateMultiPolygonGraph
{
    public static Edge[] GenerateMultiPolygonGraphSvg(
    List<NetTopologySuite.Geometries.Polygon> ignor, 
    Dictionary<NetTopologySuite.Geometries.Polygon, List<Point>> pointsByPolygon,
    Delaunator voronator, 
        float hexSize )
{
    var result = new List<Edge>();

    var triangles = voronator.GetTriangles();

    var sr = HexagonalGridGenerator.CalculateExpectedHexDistance(hexSize);

    foreach (var triangle in triangles)
    {
        for (int i = 0; i < 3; i++)
        {
            var tPoints = triangle.ToList();
            var t1 = tPoints[i];
            var t2 = tPoints[(i + 1) % 3];

            if (sr * 1.2 < Vector2.Distance(t1, t2))
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
static NetTopologySuite.Geometries.Polygon? GetPointPolygon(Point point, Dictionary<NetTopologySuite.Geometries.Polygon, List<Point>> pointsByPolygon)
{
    foreach (var kvp in pointsByPolygon)
    {
        if (kvp.Value.Contains(point))
            return kvp.Key;
    }
    return null;
}

}