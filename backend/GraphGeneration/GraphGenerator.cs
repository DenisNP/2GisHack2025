using AntAlgorithm;
using NetTopologySuite.Geometries;
using VoronatorSharp;

namespace GraphGeneration;

public static class GraphGenerator
{
    public static Edge[] Generate(List<Polygon> polygons, List<Vector2> pois)
    {
        // Настройки гексагонального заполнения
        var settings = new HexagonalMultiPolygonGenerator.HexagonalSettings
        {
            HexSize = 1,
            Density = 1,
            UseConvexHull = false,
            AddPolygonVertices = false,
            AddEdgePoints = false,
            EdgePointSpacing = 2f
        };
        
        // Генерируем точки
        var hexPoints = HexagonalMultiPolygonGenerator.GenerateHexagonalPoints(polygons, settings);
        
        // Создаем общую диаграмму Вороного/Делоне для всех точек
        var vectorPoints = hexPoints.Select(p => new VoronatorSharp.Vector2((float)p.X, (float)p.Y)).ToArray();
        var voronator = new Voronator(vectorPoints.Concat(pois).ToArray());

        // var graphNodes = DelaunayGraph.BuildGraphFromDelaunay(voronator.Delaunator);

        var pointsByPolygon = new Dictionary<NetTopologySuite.Geometries.Polygon, List<Point>>();

        foreach (var polygon in polygons)
        {
            var polygonPoints = new NetTopologySuite.Geometries.Polygon(new LinearRing(
                polygon.Vertices.Select(v => new Coordinate(v.X, v.Y)).ToArray()));
            pointsByPolygon[polygonPoints] = polygon.Vertices.Select(v => new Point(v.X, v.Y)).ToList();
        }
        
        var svgContent = GenerateMultiPolygonGraph.GenerateMultiPolygonGraphSvg(pointsByPolygon.Keys.ToList(), pointsByPolygon, voronator.Delaunator, settings.HexSize);

        return svgContent;
    }
}