using AntAlgorithm;
using GraphGeneration.A;
using NetTopologySuite.Geometries;
using VoronatorSharp;
using Point = NetTopologySuite.Geometries.Point;

namespace GraphGeneration;

public static class GraphGenerator
{
    public static Edge[] GenerateEdges(List<Polygon> polygons, List<Vector2> pois)
    {
        // Настройки гексагонального заполнения
        var settings = new HexagonalMultiPolygonGenerator.HexagonalSettings
        {
            HexSize = 4,
            Density = 1,
            UseConvexHull = false,
            AddPolygonVertices = false,
            AddEdgePoints = false,
            EdgePointSpacing = 2f
        };
        
        // Генерируем точки
        var hexPoints = HexagonalMultiPolygonGenerator.GenerateHexagonalPoints(pois.Max(p=>p.Id), polygons.ToList(), settings);
        
        // Создаем общую диаграмму Вороного/Делоне для всех точек
        // var vectorPoints = hexPoints.Select(p => new Vector2((float)p.X, (float)p.Y)).ToArray();
        var voronator = new Voronator(hexPoints.Concat(pois).ToArray());

        // var graphNodes = DelaunayGraph.BuildGraphFromDelaunay(voronator.Delaunator);

        var pointsByPolygon = new Dictionary<NetTopologySuite.Geometries.Polygon, List<Point>>();
        
        var ignore  = new List<NetTopologySuite.Geometries.Polygon>();

        foreach (var polygon in polygons)
        {
            var polygonPoints = new NetTopologySuite.Geometries.Polygon(new LinearRing(
                polygon.Vertices.Select(v => new Coordinate(v.X, v.Y)).ToArray()));
            pointsByPolygon[polygonPoints] = polygon.Vertices.Select(v => new Point(v.X, v.Y)).ToList();
            
            if (polygon.Zone == ZoneType.Restricted)
                ignore.Add(polygonPoints);
        }
        
        var svgContent = GenerateMultiPolygonGraph.GenerateMultiPolygonGraphSvg(ignore, pointsByPolygon, voronator.Delaunator, settings.HexSize);

        return svgContent;
    }
    
    public static (string, Edge[]) GenerateSvg(List<Polygon> polygons, List<Vector2> poi)
    {
        // Настройки гексагонального заполнения
        var settings = new HexagonalMultiPolygonGenerator.HexagonalSettings
        {
            HexSize = 2,
            Density = 1,
            UseConvexHull = false,
            AddPolygonVertices = false,
            AddEdgePoints = false,
            EdgePointSpacing = 2f
        };
        
        // Генерируем точки
        var hexPoints = HexagonalMultiPolygonGenerator.GenerateHexagonalPoints(poi.Max(p => p.Id), polygons.ToList(), settings);
        
        // Создаем общую диаграмму Вороного/Делоне для всех точек
        // var vectorPoints = hexPoints.Select(p => new Vector2((float)p.X, (float)p.Y)).ToArray();
        var voronator = new Voronator(hexPoints.Concat(poi).ToArray());
        
        var pointsByPolygon = new Dictionary<NetTopologySuite.Geometries.Polygon, List<Point>>();

        var ignore  = new List<NetTopologySuite.Geometries.Polygon>();
        foreach (var polygon in polygons)
        {
            var polygonPoints = new NetTopologySuite.Geometries.Polygon(new LinearRing(
                polygon.Vertices.Select(v => new Coordinate(v.X, v.Y)).ToArray()));
            pointsByPolygon[polygonPoints] = polygon.Vertices.Select(v => new Point(v.X, v.Y)).ToList();
            
            if (polygon.Zone == ZoneType.Restricted)
                ignore.Add(polygonPoints);
        }
        
        var graph = VoronoiGraphAdapter.ConvertToQuickGraph(ignore, voronator, settings.HexSize);

        var shortPaths = UniquePairsLinq.GetUniquePairsLinq(poi)
            .SelectMany(pair => VoronoiPathFinder.FindPath(graph, pair.Item1, pair.Item2))
            .ToList();
        
        
        var (edge, vector) = VoronatorFilter.Get(ignore, voronator, settings.HexSize, shortPaths);
        // foreach (var pair in UniquePairsLinq.GetUniquePairsLinq(poi))
        // {
        //     VoronoiPathFinder.FindPath(voronator, pair.Item1, pair.Item2);
        // }

        
        var svgContent = GenerateSvg3.GenerateMultiPolygonGraphSvg(pointsByPolygon, vector, edge, 50, settings.HexSize);

        return (
            svgContent,
            edge.Select(e => new Edge(new Poi(e.Source.Id, e.Source.X, e.Source.Y, e.Source.Weight), new Poi(e.Target.Id, e.Target.X, e.Target.Y, e.Target.Weight))).ToArray()
            );
    }
}