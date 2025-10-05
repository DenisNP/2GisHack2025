using System.Text;
using AntAlgorithm;
using GraphGeneration.A;
using GraphGeneration.Filters;
using GraphGeneration.Geometry;
using NetTopologySuite.Geometries;
using QuickGraph;
using VoronatorSharp;
using Point = NetTopologySuite.Geometries.Point;

namespace GraphGeneration;

public static class GraphGenerator
{
    // public static Edge[] Generate(List<ZonePolygon> polygons, List<Vector2> pois)
    // {
    //     // Настройки гексагонального заполнения
    //     var settings = new HexagonalMultiPolygonGenerator.HexagonalSettings
    //     {
    //         HexSize = 4,
    //         Density = 1,
    //         UseConvexHull = false,
    //         AddPolygonVertices = false,
    //         AddEdgePoints = false,
    //         EdgePointSpacing = 2f
    //     };
    //     
    //     // Генерируем точки
    //     var hexPoints = HexagonalMultiPolygonGenerator.GenerateHexagonalPoints(pois.Max(p=>p.Id), polygons.ToList(), settings);
    //     
    //     // Создаем общую диаграмму Вороного/Делоне для всех точек
    //     // var vectorPoints = hexPoints.Select(p => new Vector2((float)p.X, (float)p.Y)).ToArray();
    //     var voronator = new Voronator(hexPoints.Concat(pois).ToArray());
    //
    //     // var graphNodes = DelaunayGraph.BuildGraphFromDelaunay(voronator.Delaunator);
    //
    //     var pointsByPolygon = new Dictionary<NetTopologySuite.Geometries.Polygon, List<Point>>();
    //     
    //     var ignore  = new List<NetTopologySuite.Geometries.Polygon>();
    //
    //     foreach (var polygon in polygons)
    //     {
    //         var polygonPoints = new NetTopologySuite.Geometries.Polygon(new LinearRing(
    //             polygon.Vertices.Select(v => new Coordinate(v.X, v.Y)).ToArray()));
    //         pointsByPolygon[polygonPoints] = polygon.Vertices.Select(v => new Point(v.X, v.Y)).ToList();
    //         
    //         if (polygon.Type == ZoneType.Restricted)
    //             ignore.Add(polygonPoints);
    //     }
    //     
    //     var svgContent = GenerateEdgesGraphDeprecate.GetEdges(ignore, pointsByPolygon, voronator.Delaunator, settings.HexSize);
    //
    //     return svgContent;
    // }
    
    public static (Edge[] Edges, int MaxLenPath) GenerateEdges(List<ZonePolygon> polygons, List<Vector2> poi)
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

        var polygonMap = PolygonHelper.GetPolygonMap(polygons);
        
        var poiFilter = new PointAllowedFilter(polygonMap.Render);
        var validPoi = poi.Where(p => !poiFilter.Skip(p)).ToList();
        var poiMaxId = validPoi.Max(p => p.Id);
        
        // Генерируем точки
        var centersUrban = polygonMap
            .Urban
            .Where(u => polygonMap.Restricted.Any(u.Intersects)) // && !polygonMap.Available.Any(u.Crosses)
            // .Select(u => new Vector2(poiMaxId++, (float)u.Centroid.X, (float)u.Centroid.Y, 0))
            .SelectMany(u => HexagonalGridGenerator.GenerateHexagonalGridInPolygon(poiMaxId, new ZonePolygon(u, ZoneType.Urban), 4))
            .ToList();
        poiMaxId += centersUrban.Count;
        var generatedHexPoints = HexagonalMultiPolygonGenerator.GenerateHexagonalPoints(poiMaxId, polygonMap, settings);
  
        
        // Создаем общую диаграмму Вороного/Делоне для всех точек
        var voronator = new Voronator(generatedHexPoints.Concat(validPoi).Concat(centersUrban).ToArray());
        
        // Строим граф для а*
        var graph = VoronatorToQuickGraphAdapter.ConvertToQuickGraph(polygonMap, voronator, settings.HexSize);
        var originPoints = graph.Vertices.ToArray();
        var originEdges = graph.Edges.ToArray();

#if DEBUG
        // рисуем исходный граф
        var svgOriginGraph = GenerateSvg.Generate(polygonMap, originPoints, originEdges);
        File.WriteAllText("origin_graph.svg", svgOriginGraph, Encoding.UTF8);
#endif

        // ищем короткие пути между всеми парами POI
        var shortPathPoint = new List<Vector2>(originPoints.Length);
        var shortEdges = new HashSet<IEdge<Vector2>>(originEdges.Length);
        var maxLenPath = 0;
        validPoi = originPoints.Where(p => p.IsPoi).ToList();
        foreach (var pair in PointPairsHelper.GetUniquePairs(validPoi))
        {
            var shortPath = QuickPathFinder
                .FindPath(graph, pair.Item1, pair.Item2)
                .ToList();
            maxLenPath = Math.Max(maxLenPath, shortPath.Count);
            shortPathPoint.AddRange(shortPath);
            foreach (IEdge<Vector2> edge in PointPairsHelper.GetEdges(shortPath))
            {
                shortEdges.Add(edge);
            }
        }

#if DEBUG
        // рисуем короткие пути
        var svgShortPaths = GenerateFilteredSvg.Generate(polygonMap, shortPathPoint, shortEdges, 25, settings.HexSize);
        File.WriteAllText("short_paths.svg", svgShortPaths, Encoding.UTF8);
#endif
        // восстанавливаем соседей
        // var (edges, points) = VoronatorNeighborsRecover.Get(polygonMap, voronator, settings.HexSize, shortPathPoint);
        
        // смешиваем точки обогащенные соседями и точки с Urban+Available
        var urbanFilter = new PointAvaliableAndUrbanFilter(polygonMap);
        var recoveredPoints = shortPathPoint.Concat(originPoints.Where(p => !urbanFilter.Skip(p))).ToList();
        
        // Строим воронова по стабильным точкам и коротким путям
        var voronator2 = new Voronator(recoveredPoints);
        var graph2 = VoronatorToQuickGraphAdapter.ConvertToQuickGraph(polygonMap, voronator2, settings.HexSize);
        var originPoints2 = graph2.Vertices.ToArray();
        var originEdges2 = graph2.Edges.ToArray();

#if DEBUG
        // рисуем воронова по стабильным точкам и коротким путям
        var svgVoronRecovered = GenerateSvg.Generate(polygonMap, originPoints2, originEdges2);
        File.WriteAllText("voron_recovered.svg", svgVoronRecovered, Encoding.UTF8);
#endif

        // рисуем финальный граф
        // var svgFilteredGraph = GenerateSvg.Generate(polygonMap, points, edges);
        // File.WriteAllText("filtered_graph.svg", svgFilteredGraph, Encoding.UTF8);

        var resultEdges = originEdges2
            .Select(e => new Edge(
                new Poi(e.Source.Id, e.Source.X, e.Source.Y, e.Source.Weight),
                new Poi(e.Target.Id, e.Target.X, e.Target.Y, e.Target.Weight))
            ).ToArray(); 
        return (resultEdges, maxLenPath);
    }
}