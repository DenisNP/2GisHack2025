using System.Text;
using AntAlgorithm;
using GraphGeneration.A;
using GraphGeneration.Filters;
using NetTopologySuite.Geometries;
using QuickGraph;
using VoronatorSharp;
using Point = NetTopologySuite.Geometries.Point;

namespace GraphGeneration;

public static class GraphGenerator
{
    public static Edge[] Generate(List<Polygon> polygons, List<Vector2> pois)
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
        
        var svgContent = GenerateEdgesGraphDeprecate.GetEdges(ignore, pointsByPolygon, voronator.Delaunator, settings.HexSize);

        return svgContent;
    }
    
    public static Edge[] GenerateEdges(List<Polygon> polygons, List<Vector2> poi)
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
        
        var pointsByPolygon = new Dictionary<NetTopologySuite.Geometries.Polygon, List<Point>>();

        var ignore  = new List<NetTopologySuite.Geometries.Polygon>(polygons.Count);
        var allowed  = new List<NetTopologySuite.Geometries.Polygon>(polygons.Count);
        foreach (var polygon in polygons)
        {
            var polygonPoints = new NetTopologySuite.Geometries.Polygon(new LinearRing(
                polygon.Vertices.Select(v => new Coordinate(v.X, v.Y)).ToArray()));
            pointsByPolygon[polygonPoints] = polygon.Vertices.Select(v => new Point(v.X, v.Y)).ToList();
            
            if (polygon.Zone == ZoneType.Restricted)
                ignore.Add(polygonPoints);
            else
            {
                allowed.Add(polygonPoints);
            }
        }
        
        var poiFilter = new PointAllowedFilter(allowed);
        var validPoi = poi.Where(p => !poiFilter.Skip(p)).ToList();
        // var validPoi = poi.Where(p => p.IsPoi).ToList();
        
        // Генерируем точки
        var hexPoints = HexagonalMultiPolygonGenerator.GenerateHexagonalPoints(validPoi.Max(p => p.Id), polygons.ToList(), settings);
        
        // Создаем общую диаграмму Вороного/Делоне для всех точек
        var voronator = new Voronator(hexPoints.Concat(validPoi).ToArray());
        
        var graph = VoronatorToQuickGraphAdapter.ConvertToQuickGraph(ignore, allowed, voronator, settings.HexSize);
        var originPoints = graph.Vertices.ToArray();
        var originEdges = graph.Edges.ToArray();
        var svgOriginGraph = GenerateSvg.Generate(pointsByPolygon, originPoints, originEdges, 25);
        File.WriteAllText("origin_graph.svg", svgOriginGraph, Encoding.UTF8);

        // var shortPaths = PointPairsHelper.GetUniquePairs(poi)
        //     .SelectMany(pair => QuickPathFinder.FindPath(graph, pair.Item1, pair.Item2))
        //     .ToList();

        var shortPathPoint = new List<Vector2>(originPoints.Length);
        var shortEdges = new List<IEdge<Vector2>>(originEdges.Length);
        foreach (var pair in PointPairsHelper.GetUniquePairs(validPoi))
        {
            var shortPath = QuickPathFinder
                .FindPath(graph, pair.Item1, pair.Item2)
                .ToList();
            shortPathPoint.AddRange(shortPath);
            shortEdges.AddRange(PointPairsHelper.GetEdges(shortPath));
        }

        // var filteredPoints = shortPathPoint.ToArray();
        
        var svgShortPaths = GenerateFilteredSvg.Generate(ignore, allowed, pointsByPolygon, shortPathPoint, shortEdges, 25, settings.HexSize);
        File.WriteAllText("short_paths.svg", svgShortPaths, Encoding.UTF8);
        
        var (edges, points) = VoronatorNeighborsRecover.Get(ignore, allowed, voronator, settings.HexSize, shortPathPoint);
        
        var svgFilteredGraph = GenerateSvg.Generate(pointsByPolygon, points, edges, 25);
        File.WriteAllText("filtered_graph.svg", svgFilteredGraph, Encoding.UTF8);

        return edges
            .Select(e => new Edge(
                new Poi(e.Source.Id, e.Source.X, e.Source.Y, e.Source.Weight),
                new Poi(e.Target.Id, e.Target.X, e.Target.Y, e.Target.Weight))
            ).ToArray();
    }
}