using System.Text;
using GraphGeneration.AStar;
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
    private const double startClusterDistanceInMeters = 5f;
    private const double maxClusterDistanceInMeters = 15f;
    private const int maxClustersNumber = 20;
    
    // Новые константы для фильтрации маршрутов
// Новые константы для фильтрации маршрутов по медианной дистанции
    private const double commonPointsThreshold = 0.3; // 30% общих точек
    private const double medianDistanceThreshold = 2.0; // Максимальная медианная дистанция для группировки маршрутов (в метрах)
    private const int minPointsForMedianComparison = 3; // Минимальное количество точек для сравнения медиан
    private const int minPathLengthForFiltering = 5; // Минимальная длина маршрута для фильтрации

    public static GeomPoint[] GenerateEdges(List<ZonePolygon> polygons, List<GeomPoint> poi)
    {
        PolygonMap polygonMap = PolygonHelper.GetPolygonMap(polygons);
        double avArea = polygonMap.Available.Sum(p => p.Area);
        double side = Math.Sqrt(avArea);

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
        
        // Кластеризуем POI
        double currentMaxClusterDistance = startClusterDistanceInMeters;
        var pois = originPoints.Where(p => p.IsPoi).ToList();

        List<List<GeomPoint>> clusters = pois.Select(p => new List<GeomPoint>{p}).ToList();
        Console.WriteLine("Clusters: " + clusters.Count);
        while (clusters.Count > maxClustersNumber && currentMaxClusterDistance < maxClusterDistanceInMeters)
        {
            currentMaxClusterDistance = Math.Min(currentMaxClusterDistance * 1.2, maxClusterDistanceInMeters);
            clusters = GeomHelper.Clusterize(pois, polygonMap, currentMaxClusterDistance);
            
            Console.WriteLine("Clusters: " + clusters.Count + "; distance: " + currentMaxClusterDistance);
        }
        
        var centroids = clusters.Select(GeomHelper.GetMainClusterPoint).ToList();
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
        var pathsByPois = new Dictionary<(int, int), List<GeomPoint>>();
        var pointAllowedFilter = new PointAllowedFilter(polygonMap.Available);
        
        foreach (var pair in pairs)
        {
            List<GeomPoint> path = QuickPathFinder.FindPath(originPoints, neighbors, pair.Item1, pair.Item2);
            
            var key = (pair.Item1.Id, pair.Item2.Id);
            if (!pathsByPois.ContainsKey(key))
            {
                pathsByPois[key] = path
                    .Where(e => !e.IsPoi && !pointAllowedFilter.Skip(e.AsVector2()))
                    // Увеличиваем влияние точек пути
                    .Select(p =>
                    {
                        // p.AddPath(key);
                        p.Influence += pair.Item1.Weight + pair.Item2.Weight;
                        // p.Show = true;
                        return p;
                    }).ToList();
            }
        }

        Console.WriteLine($"Построено {pathsByPois.Count} маршрутов");

        // Фильтруем маршруты с общими точками
        if (pathsByPois.Count > 1)
        {
            Console.WriteLine("Фильтруем маршруты с общими точками...");
            pathsByPois = FilterPathsByCommonPoints(pathsByPois);
            Console.WriteLine($"После фильтрации осталось {pathsByPois.Count} маршрутов");
        }

#if DEBUG
        // рисуем граф после фильтрации маршрутов
        var svgFilteredGraph = GenerateSvg.Generate(
            polygonMap,
            originPoints.Where(p => !p.IsPoi || centroids.Any(c => c.Id == p.Id)).ToList(),
            originEdges.ToList()
        );
        File.WriteAllText("filtered_graph.svg", svgFilteredGraph, Encoding.UTF8);
#endif

        // var pointAllowedFilter = new PointAllowedFilter(polygonMap.Available);
        return pathsByPois.Values
            .SelectMany(p => p)
            .Where(e => e.Show && !e.IsPoi && !pointAllowedFilter.Skip(e.AsVector2()))
            .ToArray();
    }
    
    /// <summary>
/// Фильтрует маршруты с общими точками или близкой медианной дистанцией, оставляя только маршруты с максимальным средним Influence
/// </summary>
private static Dictionary<(int, int), List<GeomPoint>> FilterPathsByCommonPoints(
    Dictionary<(int, int), List<GeomPoint>> pathsByPois)
{
    var pathGroups = new List<List<KeyValuePair<(int, int), List<GeomPoint>>>>();
    var processedKeys = new HashSet<(int, int)>();
    
    // Константы для группировки по медианной дистанции
    const double medianDistanceThreshold = 5; // Максимальная медианная дистанция для группировки маршрутов
    const int minPointsForMedianComparison = 3; // Минимальное количество точек для сравнения медиан
    
    // Группируем маршруты по общим точкам или близкой медианной дистанции
    foreach (var path1 in pathsByPois)
    {
        if (processedKeys.Contains(path1.Key)) continue;
        
        var group = new List<KeyValuePair<(int, int), List<GeomPoint>>> { path1 };
        processedKeys.Add(path1.Key);
        
        // Находим все маршруты с общими точками или близкой медианной дистанцией
        for (int i = 0; i < group.Count; i++)
        {
            var currentPath = group[i];
            
            foreach (var path2 in pathsByPois)
            {
                if (processedKeys.Contains(path2.Key)) continue;
                
                double commonPointsRatio = CalculateCommonPointsRatio(currentPath, path2);
                double medianDistance = CalculateMedianDistanceBetweenPaths(currentPath.Value, path2.Value);
                
                bool shouldGroup = commonPointsRatio >= commonPointsThreshold || 
                                 (medianDistance <= medianDistanceThreshold && 
                                  currentPath.Value.Count >= minPointsForMedianComparison && 
                                  path2.Value.Count >= minPointsForMedianComparison);
                
                if (shouldGroup)
                {
                    group.Add(path2);
                    processedKeys.Add(path2.Key);
                    
                    Console.WriteLine($"Группируем маршруты {currentPath.Key} и {path2.Key}: " +
                                    $"общие точки = {commonPointsRatio:P0}, " +
                                    $"медианная дистанция = {medianDistance:F2}m");
                }
            }
        }
        
        pathGroups.Add(group);
    }
    
    // Для каждой группы оставляем только маршрут с максимальным средним Influence
    var result = new Dictionary<(int, int), List<GeomPoint>>();
    
    foreach (var group in pathGroups)
    {
        if (group.Count == 1)
        {
            // Если в группе только один маршрут, просто добавляем его
            result.Add(group[0].Key, group[0].Value);
            group[0].Value.ForEach(p => p.Show = true);
        }
        else
        {
            Console.WriteLine($"Группа из {group.Count} маршрутов:");
            foreach (var path in group)
            {
                double avgInfluence = CalculateAverageInfluence(path.Value);
                Console.WriteLine($"  Маршрут {path.Key}: средний Influence = {avgInfluence:F2}, точек = {path.Value.Count}");
            }
            
            // Находим маршрут с максимальным средним Influence
            var bestPath = group
                .Select(path => new {
                    Path = path,
                    AvgInfluence = CalculateAverageInfluence(path.Value),
                    PathLength = path.Value.Count
                })
                .OrderByDescending(x => x.AvgInfluence)
                .ThenByDescending(x => x.PathLength) // При равном Influence предпочитаем более длинные маршруты
                .First();
            
            Console.WriteLine($"  Выбран маршрут {bestPath.Path.Key} со средним Influence: {bestPath.AvgInfluence:F2}");
            
            // Добавляем только лучший маршрут
            result.Add(bestPath.Path.Key, bestPath.Path.Value);
            bestPath.Path.Value.ForEach(p => p.Show = true);
        }
    }
    
    return result;
}

/// <summary>
/// Вычисляет медианную дистанцию между двумя маршрутами
/// </summary>
private static double CalculateMedianDistanceBetweenPaths(List<GeomPoint> path1, List<GeomPoint> path2)
{
    // Исключаем POI точки из расчета
    var nonPoiPoints1 = path1.Where(p => !p.IsPoi).ToList();
    var nonPoiPoints2 = path2.Where(p => !p.IsPoi).ToList();
    
    if (nonPoiPoints1.Count == 0 || nonPoiPoints2.Count == 0)
        return double.MaxValue;
    
    // Для каждой точки в первом маршруте находим минимальное расстояние до второго маршрута
    var minDistances = new List<double>();
    
    foreach (var point1 in nonPoiPoints1)
    {
        double minDistance = nonPoiPoints2.Min(point2 => 
            CalculateDistance(point1.AsVector2(), point2.AsVector2()));
        minDistances.Add(minDistance);
    }
    
    // Вычисляем медиану минимальных расстояний
    if (minDistances.Count == 0)
        return double.MaxValue;
    
    minDistances.Sort();
    int mid = minDistances.Count / 2;
    
    if (minDistances.Count % 2 == 0)
    {
        return (minDistances[mid - 1] + minDistances[mid]) / 2.0;
    }
    else
    {
        return minDistances[mid];
    }
}

/// <summary>
/// Вычисляет евклидово расстояние между двумя точками
/// </summary>
private static double CalculateDistance(Vector2 a, Vector2 b)
{
    double dx = a.X - b.X;
    double dy = a.Y - b.Y;
    return Math.Sqrt(dx * dx + dy * dy);
}   
    
    /// <summary>
    /// Вычисляет отношение общих точек между двумя маршрутами
    /// </summary>
    private static double CalculateCommonPointsRatio(KeyValuePair<(int, int), List<GeomPoint>> path1, KeyValuePair<(int, int), List<GeomPoint>> path2)
    {
        // Исключаем POI точки из расчета
        var nonPoiPoints1 = path1.Value.Select(p => p.Id).ToHashSet();
        var nonPoiPoints2 = path2.Value.Select(p => p.Id).ToHashSet();
        
        if (nonPoiPoints1.Count == 0 || nonPoiPoints2.Count == 0)
            return 0;
        
        int commonPointsCount = nonPoiPoints1.Intersect(nonPoiPoints2).Count();
        int minPathLength = Math.Min(nonPoiPoints1.Count, nonPoiPoints2.Count);
        
        double ratio = (double)commonPointsCount / minPathLength;
        
        // Логируем для отладки
        if (ratio >= commonPointsThreshold)
        {
            Console.WriteLine($"{path1.Key} - {path2.Key} Общие точки: {commonPointsCount}/{minPathLength} ({ratio:P0})");
        }
        
        return ratio;
    }
    
    /// <summary>
    /// Вычисляет среднее Influence для маршрута (исключая POI)
    /// </summary>
    private static double CalculateAverageInfluence(List<GeomPoint> path)
    {
        var nonPoiPoints = path.Where(p => !p.IsPoi).ToList();
        if (nonPoiPoints.Count == 0) return 0;
        
        return nonPoiPoints.Average(p => p.Influence);
    }
    
    /// <summary>
    /// Строит словарь соседей для графа
    /// </summary>
    private static Dictionary<int, List<(GeomPoint neighbor, double cost)>> BuildNeighborsDictionary(
        List<GeomPoint> points, List<GeomEdge> edges)
    {
        var neighbors = new Dictionary<int, List<(GeomPoint neighbor, double cost)>>();
        
        foreach (var point in points)
        {
            neighbors[point.Id] = new List<(GeomPoint, double)>();
        }
        
        foreach (var edge in edges)
        {
            neighbors[edge.From.Id].Add((edge.To, edge.Cost()));
            neighbors[edge.To.Id].Add((edge.From, edge.Cost()));
        }
        
        return neighbors;
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