using System.Text;
using GraphGeneration.Filters;
using GraphGeneration.Geometry;
using GraphGeneration.Models;
using GraphGeneration.Svg;
using GraphGeneration.VoronatorGraph;
using PathScape.Domain.Models;
using VoronatorSharp;

namespace GraphGeneration;

public static class AdvancedGraphGenerator
{
    private const float minSmallHexSize = 0.5f;
    private const float maxSmallHexSize = 3f;
    private const float sideToHexRatio = 200f;
    private const float minBigHexSize = 3f;
    private const float maxBigHexSize = 5f;
    private const float bigHexToSmallRatio = 3f;
    private static double _startClusterDistanceInMeters;
    private static double _maxClusterDistanceInMeters;
    private const int maxClustersNumber = 20;

    public static GeomPoint[] GenerateEdges(List<ZonePolygon> polygons, List<GeomPoint> poi)
    {
        PolygonMap polygonMap = PolygonHelper.GetPolygonMap(polygons);
        double avArea = polygonMap.Available.Sum(p => p.Area);
        double side = Math.Sqrt(avArea);

        _startClusterDistanceInMeters = side / 30;
        _maxClusterDistanceInMeters = side / 5;

        Console.WriteLine("Total Area: " + avArea + "; side: " + side);
        float hexSize = Math.Clamp((float)side / sideToHexRatio, minSmallHexSize, maxSmallHexSize);
        float bigHexSize = Math.Clamp(hexSize * bigHexToSmallRatio, minBigHexSize, maxBigHexSize);

        // Настройки гексагонального заполнения
        var settings = new HexagonalMultiPolygonGenerator.HexagonalSettings
        {
            HexSize = hexSize,
            Density = 1,
            UseConvexHull = false,
            AddPolygonVertices = false,
            AddEdgePoints = false,
            EdgePointSpacing = 2f
        };
        
        var poiFilter = new PointAllowedFilter(polygonMap.Render);
        List<GeomPoint> validPoi = poi.Where(p => !poiFilter.Skip(p.AsVector2())).ToList();
        int poiMaxId = validPoi.Max(p => p.Id);

        // Генерируем точки
        List<Vector2> urban = polygonMap
            .Urban
            .Where(u => polygonMap.Restricted.Any(u.Intersects)) 
            .SelectMany(u => HexagonalGridGenerator.GenerateHexagonalGridInPolygon(ref poiMaxId, new ZonePolygon(u, ZoneType.Urban), bigHexSize, forceCentroidIfEmpty: true))
            .ToList();

        List<Vector2> generatedHexPoints = HexagonalMultiPolygonGenerator.GenerateHexagonalPoints(ref poiMaxId, polygonMap, settings);
        List<Vector2> generatedBigHexPoints = HexagonalMultiPolygonGenerator.GenerateSpacedHexagonalPointsOutside(ref poiMaxId, polygonMap, bigHexSize);

        // Создаем общую диаграмму Вороного/Делоне для всех точек
        var allPoints = generatedHexPoints
            .Concat(validPoi.Select(x => x.AsVector2()))
            .Concat(urban)
            .Concat(generatedBigHexPoints);

        var voronator = new Voronator(allPoints.ToArray());

        // Строим граф для а*
        (List<GeomPoint> originPoints, List<GeomEdge> originEdges) = VoronatorToGeomAdapter.ConvertToQuickGraph(1, polygonMap, voronator, settings.HexSize);

        // Построение графа смежности
        var neighbors = new Dictionary<int, List<(GeomPoint neighbor, double cost)>>();
        foreach (var point in originPoints)
        {
            neighbors[point.Id] = new List<(GeomPoint, double)>();
        }
        
        foreach (var edge in originEdges)
        {
            neighbors[edge.From.Id].Add((edge.To, edge.Cost()));
            neighbors[edge.To.Id].Add((edge.From, edge.Cost()));
        }

#if DEBUG
        // рисуем исходный граф
        var svgOriginGraph = GenerateSvg.Generate(polygonMap, originPoints, originEdges);
        File.WriteAllText("origin_graph.svg", svgOriginGraph, Encoding.UTF8);
#endif
        // Подготавливаем сетку для симуляции
        
        // Кластеризуем
        double currentMaxClusterDistance = _startClusterDistanceInMeters;
        var pois = originPoints.Where(p => p.IsPoi).ToList();

        List<List<GeomPoint>> clusters = pois.Select(p => new List<GeomPoint>{p}).ToList();
        Console.WriteLine("Clusters: " + clusters.Count);
        while (clusters.Count > maxClustersNumber && currentMaxClusterDistance < _maxClusterDistanceInMeters)
        {
            currentMaxClusterDistance = Math.Min(currentMaxClusterDistance * 1.2, _maxClusterDistanceInMeters);
            clusters = GeomHelper.Clusterize(pois, polygonMap, currentMaxClusterDistance);
            
            Console.WriteLine("Clusters: " + clusters.Count + "; distance: " + currentMaxClusterDistance);
        }
        
        List<GeomPoint> centroids = clusters.Select(GeomHelper.GetMainClusterPoint).ToList();
        List<(GeomPoint, GeomPoint)> pairs = GeneratePoiPairs(centroids, polygonMap).ToList();
        Console.WriteLine("Pairs: " + pairs.Count);

#if DEBUG
        // рисуем исходный граф
        var svgClustersGraph = GenerateSvg.Generate(
            polygonMap,
            originPoints.Where(p => !p.IsPoi || centroids.Any(c => c.Id == p.Id)).ToList(),
            originEdges.ToList()
        );
        File.WriteAllText("clusters_graph.svg", svgClustersGraph, Encoding.UTF8);
#endif

        // строим маршруты
        var paths = Optimizer.Run(originPoints, centroids, pairs, neighbors, polygonMap, hexSize);
        
        // Удаляем пути вне нужных зон
        var pointAllowedFilter = new PointAllowedFilter(polygonMap.Available);
        paths.ForEach(path => path.RemoveAll(p => p.IsPoi || pointAllowedFilter.Skip(p.AsVector2())));
        paths.RemoveAll(path => path.Count == 0);

        Console.WriteLine("Paths got: " + paths.Count);

#if DEBUG
        // рисуем исходный граф
        var svgPathsGraph = GenerateSvg.Generate(
            polygonMap,
            originPoints.Where(p => !p.IsPoi || centroids.Any(c => c.Id == p.Id)).ToList(),
            originEdges.ToList()
        );
        File.WriteAllText("paths_graph.svg", svgPathsGraph, Encoding.UTF8);
#endif
        
        // Возвращаем верхние N по среднему влиянию пути
        int pathsToReturn = Math.Clamp((int)Math.Round(0.4 * paths.Count + 0.3 * side - 36), 10, 100);
        Console.WriteLine("Paths to return " + pathsToReturn);

        var pathsWithInfluence = paths
            .Select(path => (path, influence: path.Average(p => p.Influence)))
            .OrderByDescending(path => path.influence)
            .Take(pathsToReturn);

        foreach (var point in pathsWithInfluence.SelectMany(path => path.path))
        {
            point.Show = true;
        }
        
        // нормализуем
        double maxInfluence = paths.SelectMany(p => p).Select(p => p.Influence).Max();
        originPoints.ForEach(p => p.Influence /= maxInfluence);

        // возвращаем
        return originPoints.Where(p => p.Show).ToArray();
    }

    private static IEnumerable<(GeomPoint, GeomPoint)> GenerateUniqPairs(List<GeomPoint> pois)
    {
        for (int i = 0; i < pois.Count; i++)
        {
            for (int j = i + 1; j < pois.Count; j++)
            {
                yield return (pois.ElementAt(i), pois.ElementAt(j));
            }
        }
    }

    private static IEnumerable<(GeomPoint, GeomPoint)> GeneratePoiPairs(List<GeomPoint> pois, PolygonMap polygonMap)
    {
        var pairs = GenerateUniqPairs(pois).ToList();
        pairs.RemoveAll(p => !PolygonHelper.IsPairCrossesAvailable(p.Item1, p.Item2, polygonMap));

        return pairs;
    }
}