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
    private const double startClusterDistanceInMeters = 5f;
    private const double maxClusterDistanceInMeters = 20f;
    private const int maxClustersNumber = 20;
    
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
        var voronator = new Voronator(generatedHexPoints.Concat(validPoi.Select(x => x.AsVector2())).Concat(urban).Concat(generatedBigHexPoints).ToArray());

        // Строим граф для а*
        (List<GeomPoint> originPoints, List<GeomEdge> originEdges) = VoronatorToGeomAdapter.ConvertToQuickGraph(poiMaxId + 1, polygonMap, voronator, settings.HexSize);

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
        List<(GeomPoint, GeomPoint)> pairs = GeneratePoiPairs(originPoints.Where(p => p.IsPoi).ToList(), neighbors, polygonMap).ToList();
        Console.WriteLine("Pairs: " + pairs.Count);
        
        // Кластеризуем
        double currentMaxClusterDistance = startClusterDistanceInMeters;
        List<List<GeomPoint>> clusters = originPoints.Select(p => new List<GeomPoint>{p}).ToList();
        while (clusters.Count > maxClustersNumber && currentMaxClusterDistance < maxClusterDistanceInMeters)
        {
            currentMaxClusterDistance = Math.Min(currentMaxClusterDistance * 1.2, maxClusterDistanceInMeters);
            clusters = GeomHelper.Clusterize(originPoints, polygonMap, currentMaxClusterDistance);
        }

#if DEBUG
        // рисуем исходный граф
        var svgShortGraph = GenerateSvg.Generate(polygonMap, clusters.Select(GeomHelper.GetCentroid).ToList(), originEdges.ToList());
        File.WriteAllText("clusters_graph.svg", svgShortGraph, Encoding.UTF8);
#endif

        var pointAllowedFilter = new PointAllowedFilter(polygonMap.Available);
        return originPoints
            .Where(e => e.Show && !e.IsPoi && !pointAllowedFilter.Skip(e.AsVector2()))
            .ToArray();
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

    private static IEnumerable<(GeomPoint, GeomPoint)> GeneratePoiPairs(List<GeomPoint> pois, Dictionary<int, List<(GeomPoint neighbor, double cost)>> neighbors, PolygonMap polygonMap)
    {
        var pairs = GenerateUniqPairs(pois).ToList();
        pairs.RemoveAll(p =>
        {
            var n = neighbors[p.Item1.Id];
            return n.Any(x => x.neighbor.Id == p.Item2.Id);
        });
        pairs.RemoveAll(p => PolygonHelper.IsPairCrossesAvailable(p.Item1, p.Item2, polygonMap));

        return pairs;
    }
}