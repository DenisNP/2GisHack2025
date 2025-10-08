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
    private const double medianDistanceThreshold = 8.0; // Максимальная медианная дистанция для группировки маршрутов (в метрах)
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
/// <summary>
/// Фильтрует маршруты с общими точками или близкой медианной дистанцией, оставляя лучшие сегменты маршрутов
/// </summary>
/// <summary>
/// Фильтрует маршруты: сначала по группам целиком, потом по сегментам для оставшихся
/// </summary>
private static Dictionary<(int, int), List<GeomPoint>> FilterPathsByCommonPoints(
    Dictionary<(int, int), List<GeomPoint>> pathsByPois)
{
    // Сначала сбрасываем все Show = false для точек маршрутов (кроме POI)
    // foreach (var route in pathsByPois.Values)
    // {    
    //     foreach (var point in route.Where(p => !p.IsPoi))
    //     {
    //         point.Show = false;
    //     }
    // }

    // Этап 1: Группируем маршруты целиком
    var groupedPaths = GroupPathsByCommonPointsAndDistance(pathsByPois);
    Console.WriteLine($"После группировки маршрутов целиком: {groupedPaths.Count} маршрутов");

    // Этап 2: Анализируем оставшиеся маршруты на уровне сегментов
    AnalyzeRemainingPathsBySegments(groupedPaths);
    
    return groupedPaths;
}

/// <summary>
/// Этап 1: Группировка маршрутов целиком по общим точкам и медианной дистанции
/// </summary>
private static Dictionary<(int, int), List<GeomPoint>> GroupPathsByCommonPointsAndDistance(
    Dictionary<(int, int), List<GeomPoint>> pathsByPois)
{
    var pathGroups = new List<List<KeyValuePair<(int, int), List<GeomPoint>>>>();
    var processedKeys = new HashSet<(int, int)>();
    
    // Группируем маршруты по общим точкам или близкой медианной дистанции
    foreach (var path1 in pathsByPois)
    {
        if (processedKeys.Contains(path1.Key)) continue;
        
        var group = new List<KeyValuePair<(int, int), List<GeomPoint>>> { path1 };
        processedKeys.Add(path1.Key);
        
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
            // MarkPointsAsVisible(group[0].Value);
        }
        else
        {
            Console.WriteLine($"Группа из {group.Count} маршрутов с общими точками:");
            
            // Находим маршрут с максимальным средним Influence
            var bestPath = group
                .Select(path => new {
                    Path = path,
                    AvgInfluence = CalculateAverageInfluence(path.Value),
                    PathLength = path.Value.Count
                })
                .OrderByDescending(x => x.AvgInfluence)
                .ThenByDescending(x => x.PathLength)
                .First();
            
            Console.WriteLine($"  Выбран маршрут {bestPath.Path.Key} со средним Influence: {bestPath.AvgInfluence:F2}");
            
            // Добавляем только лучший маршрут и помечаем его точки как видимые
            result.Add(bestPath.Path.Key, bestPath.Path.Value);
            // MarkPointsAsVisible(bestPath.Path.Value);
            
            // Остальные маршруты в группе не добавляем в результат, их точки остаются Show = false
        }
    }
    
    return result;
}

/// <summary>
/// Этап 2: Анализ оставшихся маршрутов на уровне сегментов
/// </summary>
private static void AnalyzeRemainingPathsBySegments(
    Dictionary<(int, int), List<GeomPoint>> remainingPaths)
{
    if (remainingPaths.Count < 2)
    {
        Console.WriteLine("Слишком мало маршрутов для анализа сегментов");
        return;
    }

    Console.WriteLine($"Анализируем {remainingPaths.Count} маршрутов на уровне сегментов...");

    // Находим все общие точки между всеми оставшимися маршрутами
    var commonPoints = FindCommonPointsAcrossAllRoutes(remainingPaths);
    Console.WriteLine($"Найдено {commonPoints.Count} общих точек между всеми маршрутами");

    // Разбиваем все маршруты на сегменты по общим точкам
    var allSegments = new List<RouteSegment>();
    // var segmentsByRoute = new Dictionary<(int, int), List<RouteSegment>>();

    foreach (var route in remainingPaths)
    {
        var segments = SplitRouteIntoSegments(route.Value, commonPoints);
        // segmentsByRoute[route.Key] = segments;
        allSegments.AddRange(segments);
        
        Console.WriteLine($"  Маршрут {route.Key} разбит на {segments.Count} сегментов");
        if (segments.Count == 0)
            MarkPointsAsVisible(route.Value);
    }

    // Группируем схожие сегменты по конечным точкам и медианной дистанции
    var segmentGroups = GroupSimilarSegmentsAcrossAllRoutes(allSegments);
    Console.WriteLine($"Создано {segmentGroups.Count} групп сегментов");

    // Для каждой группы сегментов выбираем лучший и помечаем его точки как видимые
    MarkBestSegmentsAsVisible(segmentGroups);
}

/// <summary>
/// Помечает точки лучших сегментов как видимые
/// </summary>
private static void MarkBestSegmentsAsVisible(List<List<RouteSegment>> segmentGroups)
{
    foreach (var group in segmentGroups)
    {
        if (group.Count == 0) continue;

        // Выбираем сегмент с максимальным средним Influence
        var bestSegment = group
            .OrderByDescending(s => s.AvgInfluence)
            .ThenByDescending(s => s.Points.Count)
            .First();

        // Помечаем точки лучшего сегмента как видимые
        MarkPointsAsVisible(bestSegment.Points);

        Console.WriteLine($"  Лучший сегмент {bestSegment.StartPointId}-{bestSegment.EndPointId}: " +
                        $"Influence={bestSegment.AvgInfluence:F2}, точек={bestSegment.Points.Count}");

        // Остальные сегменты в группе остаются с Show = false (уже сброшено в начале)
    }
}

/// <summary>
/// Находит общие точки между всеми маршрутами
/// </summary>
private static HashSet<int> FindCommonPointsAcrossAllRoutes(Dictionary<(int, int), List<GeomPoint>> routes)
{
    var commonPoints = new HashSet<int>();
    
    if (routes.Count < 2) return commonPoints;

    // Собираем все точки из всех маршрутов
    var allPoints = routes.Values.SelectMany(route => route.Select(p => p.Id)).ToList();
    
    // Находим точки, которые встречаются в нескольких маршрутах
    var pointFrequency = new Dictionary<int, int>();
    foreach (var pointId in allPoints)
    {
        pointFrequency[pointId] = pointFrequency.GetValueOrDefault(pointId) + 1;
    }

    // Считаем общими точки, которые встречаются хотя бы в 2 маршрутах
    foreach (var (pointId, frequency) in pointFrequency)
    {
        if (frequency >= 2)
        {
            commonPoints.Add(pointId);
        }
    }

    return commonPoints;
}

/// <summary>
/// Группирует схожие сегменты по всем маршрутам
/// </summary>
private static List<List<RouteSegment>> GroupSimilarSegmentsAcrossAllRoutes(List<RouteSegment> allSegments)
{
    var segmentGroups = new List<List<RouteSegment>>();
    var processedSegments = new HashSet<RouteSegment>();

    foreach (var segment in allSegments)
    {
        if (processedSegments.Contains(segment)) continue;

        var group = new List<RouteSegment> { segment };
        processedSegments.Add(segment);

        // Находим все схожие сегменты
        for (int i = 0; i < group.Count; i++)
        {
            var currentSegment = group[i];

            foreach (var otherSegment in allSegments)
            {
                if (processedSegments.Contains(otherSegment)) continue;
                if (currentSegment == otherSegment) continue;

                // Сегменты считаются схожими если:
                // 1. Они имеют одинаковые стартовые и конечные точки ИЛИ
                // 2. Они имеют близкую медианную дистанцию и соединяют схожие участки
                bool areSimilar = (AreSegmentsConnectingSimilarAreas(currentSegment, otherSegment) &&
                                   CalculateMedianDistanceBetweenSegments(currentSegment, otherSegment) <= medianDistanceThreshold);

                if (areSimilar)
                {
                    group.Add(otherSegment);
                    processedSegments.Add(otherSegment);
                }
            }
        }

        segmentGroups.Add(group);
    }
    
    foreach (var routeSegment in processedSegments.Except(allSegments))
    {
        MarkPointsAsVisible(routeSegment.Points);
    }

    return segmentGroups;
}

/// <summary>
/// Проверяет, соединяют ли сегменты схожие области
/// </summary>
private static bool AreSegmentsConnectingSimilarAreas(RouteSegment seg1, RouteSegment seg2)
{
    // Простая проверка: сегменты считаются соединяющими схожие области,
    // если их начальные и конечные точки находятся близко друг к другу
    var seg1Start = seg1.Points.First().AsVector2();
    var seg1End = seg1.Points.Last().AsVector2();
    var seg2Start = seg2.Points.First().AsVector2();
    var seg2End = seg2.Points.Last().AsVector2();

    double startDistance = CalculateDistance(seg1Start, seg2Start);
    double endDistance = CalculateDistance(seg1End, seg2End);

    return startDistance <= 10 && endDistance <= 10;
}



// /// <summary>
// /// Анализирует сегменты маршрутов и выбирает лучшие для каждого участка
// /// </summary>
// private static Dictionary<(int, int), List<RouteSegment>> AnalyzeAndSelectBestSegments(
//     List<KeyValuePair<(int, int), List<GeomPoint>>> group)
// {
//     // Находим все общие точки между маршрутами в группе
//     var commonPoints = FindCommonPointsInGroup(group);
//     
//     // Разбиваем каждый маршрут на сегменты по общим точкам
//     var segmentsByRoute = new Dictionary<(int, int), List<RouteSegment>>();
//     
//     foreach (var route in group)
//     {
//         var segments = SplitRouteIntoSegments(route.Value, commonPoints);
//         segmentsByRoute[route.Key] = segments;
//         
//         Console.WriteLine($"  Маршрут {route.Key} разбит на {segments.Count} сегментов");
//     }
//     
//     // Группируем схожие сегменты по медианной дистанции
//     var segmentGroups = GroupSimilarSegments(segmentsByRoute, commonPoints);
//     
//     // Для каждой группы сегментов выбираем лучший
//     var bestSegmentsByRoute = SelectBestSegmentsForEachRoute(segmentsByRoute, segmentGroups);
//     
//     return bestSegmentsByRoute;
// }

// /// <summary>
// /// Находит общие точки между всеми маршрутами в группе
// /// </summary>
// private static HashSet<int> FindCommonPointsInGroup(List<KeyValuePair<(int, int), List<GeomPoint>>> group)
// {
//     var commonPoints = new HashSet<int>();
//     
//     if (group.Count < 2) return commonPoints;
//     
//     // Начинаем с точек первого маршрута
//     var firstRoutePoints = group[0].Value.Select(p => p.Id).ToHashSet();
//     
//     // Ищем точки, которые есть во всех маршрутах
//     foreach (var pointId in firstRoutePoints)
//     {
//         bool isCommonInAll = true;
//         
//         for (int i = 1; i < group.Count; i++)
//         {
//             var routePointIds = group[i].Value.Select(p => p.Id).ToHashSet();
//             if (!routePointIds.Contains(pointId))
//             {
//                 isCommonInAll = false;
//                 break;
//             }
//         }
//         
//         if (isCommonInAll)
//         {
//             commonPoints.Add(pointId);
//         }
//     }
//     
//     Console.WriteLine($"  Найдено {commonPoints.Count} общих точек в группе");
//     return commonPoints;
// }

/// <summary>
/// Разбивает маршрут на сегменты по общим точкам
/// </summary>
private static List<RouteSegment> SplitRouteIntoSegments(List<GeomPoint> route, HashSet<int> commonPoints)
{
    var segments = new List<RouteSegment>();
    var currentSegment = new List<GeomPoint>();
    int segmentStartIndex = 0;
    
    for (int i = 0; i < route.Count; i++)
    {
        currentSegment.Add(route[i]);
        
        // Если текущая точка общая (и не первая/последняя в маршруте), завершаем сегмент
        if (commonPoints.Contains(route[i].Id) && i > 0 && i < route.Count - 1)
        {
            if (currentSegment.Count > 1)
            {
                segments.Add(new RouteSegment
                {
                    Points = currentSegment.ToList(),
                    StartPointId = route[segmentStartIndex].Id,
                    EndPointId = route[i].Id,
                    AvgInfluence = CalculateAverageInfluence(currentSegment)
                });
            }
            
            // Начинаем новый сегмент с текущей точки
            currentSegment = new List<GeomPoint> { route[i] };
            segmentStartIndex = i;
        }
    }
    
    // Добавляем последний сегмент
    if (currentSegment.Count > 1)
    {
        segments.Add(new RouteSegment
        {
            Points = currentSegment,
            StartPointId = route[segmentStartIndex].Id,
            EndPointId = route[route.Count - 1].Id,
            AvgInfluence = CalculateAverageInfluence(currentSegment)
        });
    }
    
    return segments;
}

// /// <summary>
// /// Группирует схожие сегменты по медианной дистанции
// /// </summary>
// private static List<List<RouteSegment>> GroupSimilarSegments(
//     Dictionary<(int, int), List<RouteSegment>> segmentsByRoute,
//     HashSet<int> commonPoints)
// {
//     var allSegments = segmentsByRoute.Values.SelectMany(x => x).ToList();
//     var segmentGroups = new List<List<RouteSegment>>();
//     var processedSegments = new HashSet<RouteSegment>();
//     
//     foreach (var segment in allSegments)
//     {
//         if (processedSegments.Contains(segment)) continue;
//         
//         var group = new List<RouteSegment> { segment };
//         processedSegments.Add(segment);
//         
//         // Находим все схожие сегменты
//         for (int i = 0; i < group.Count; i++)
//         {
//             var currentSegment = group[i];
//             
//             foreach (var otherSegment in allSegments)
//             {
//                 if (processedSegments.Contains(otherSegment)) continue;
//                 if (currentSegment == otherSegment) continue;
//                 
//                 // Сегменты считаются схожими если:
//                 // 1. Они имеют одинаковые стартовые и конечные точки ИЛИ
//                 // 2. Они имеют близкую медианную дистанцию
//                 bool areSimilar = (currentSegment.StartPointId == otherSegment.StartPointId &&
//                                  currentSegment.EndPointId == otherSegment.EndPointId) ||
//                                 CalculateMedianDistanceBetweenSegments(currentSegment, otherSegment) <= medianDistanceThreshold;
//                 
//                 if (areSimilar)
//                 {
//                     group.Add(otherSegment);
//                     processedSegments.Add(otherSegment);
//                 }
//             }
//         }
//         
//         segmentGroups.Add(group);
//     }
//     
//     Console.WriteLine($"  Создано {segmentGroups.Count} групп сегментов");
//     return segmentGroups;
// }

/// <summary>
/// Выбирает лучшие сегменты для каждого маршрута
/// </summary>
private static Dictionary<(int, int), List<RouteSegment>> SelectBestSegmentsForEachRoute(
    Dictionary<(int, int), List<RouteSegment>> segmentsByRoute,
    List<List<RouteSegment>> segmentGroups)
{
    var bestSegmentsByRoute = new Dictionary<(int, int), List<RouteSegment>>();
    
    // Инициализируем для каждого маршрута пустой список сегментов
    foreach (var routeKey in segmentsByRoute.Keys)
    {
        bestSegmentsByRoute[routeKey] = new List<RouteSegment>();
    }
    
    // Для каждой группы сегментов выбираем лучший
    foreach (var segmentGroup in segmentGroups)
    {
        if (segmentGroup.Count == 0) continue;
        
        // Выбираем сегмент с максимальным средним Influence
        var bestSegment = segmentGroup
            .OrderByDescending(s => s.AvgInfluence)
            .ThenByDescending(s => s.Points.Count)
            .First();
        
        Console.WriteLine($"  Лучший сегмент: Influence={bestSegment.AvgInfluence:F2}, " +
                        $"точки={bestSegment.Points.Count}, маршрут={bestSegment.GetRouteKey()}");
        
        // Добавляем лучший сегмент в соответствующий маршрут
        var routeKey = bestSegment.GetRouteKey();
        if (bestSegmentsByRoute.ContainsKey(routeKey))
        {
            bestSegmentsByRoute[routeKey].Add(bestSegment);
        }
    }
    
    // Сортируем сегменты в каждом маршруте по порядку следования
    foreach (var routeKey in bestSegmentsByRoute.Keys.ToList())
    {
        var originalRoute = segmentsByRoute[routeKey];
        var bestSegments = bestSegmentsByRoute[routeKey];
        
        // Восстанавливаем порядок сегментов как в оригинальном маршруте
        bestSegmentsByRoute[routeKey] = bestSegments
            .OrderBy(s => originalRoute.FindIndex(seg => seg.StartPointId == s.StartPointId))
            .ToList();
    }
    
    return bestSegmentsByRoute;
}

/// <summary>
/// Восстанавливает маршрут из сегментов
/// </summary>
private static List<GeomPoint> ReconstructRouteFromSegments(List<RouteSegment> segments)
{
    if (segments.Count == 0) return new List<GeomPoint>();
    
    var route = new List<GeomPoint>();
    
    for (int i = 0; i < segments.Count; i++)
    {
        var segment = segments[i];
        
        // Для первого сегмента добавляем все точки
        if (i == 0)
        {
            route.AddRange(segment.Points);
        }
        else
        {
            // Для последующих сегментов избегаем дублирования точек соединения
            route.AddRange(segment.Points.Skip(1));
        }
    }
    
    return route;
}

/// <summary>
/// Вычисляет медианную дистанцию между двумя сегментами
/// </summary>
private static double CalculateMedianDistanceBetweenSegments(RouteSegment segment1, RouteSegment segment2)
{
    return CalculateMedianDistanceBetweenPaths(segment1.Points, segment2.Points);
}

/// <summary>
/// Помечает точки как видимые
/// </summary>
private static void MarkPointsAsVisible(List<GeomPoint> points)
{
    foreach (var point in points)
    {
        point.Show = true;
    }
}

/// <summary>
/// Модель сегмента маршрута
/// </summary>
private class RouteSegment
{
    public List<GeomPoint> Points { get; set; } = new();
    public int StartPointId { get; set; }
    public int EndPointId { get; set; }
    public double AvgInfluence { get; set; }
    
    public (int, int) GetRouteKey()
    {
        if (Points.Count == 0) return (0, 0);
        return (Points.First().Id, Points.Last().Id);
    }
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