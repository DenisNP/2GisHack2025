using System.Text;
using GraphGeneration.A;
using GraphGeneration.Filters;
using GraphGeneration.Models;
using GraphGeneration.Svg;
using PathScape.Domain.Models;
using VoronatorSharp;

namespace GraphGeneration.Geometry;

public static class GraphGenerator
{
    public static (IList<GeomEdge> Edges, HashSet<(GeomPoint, GeomPoint)> LongPaths, int MaxLenPath) GenerateEdges(List<ZonePolygon> polygons, List<Vector2> poi)
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
        var (originPoints, originEdges) = VoronatorToQuickGraphAdapter.ConvertToQuickGraph(poiMaxId + 1, polygonMap, voronator, settings.HexSize);

#if DEBUG
        // рисуем исходный граф
        var svgOriginGraph = GenerateSvg.Generate(polygonMap, originPoints, originEdges);
        File.WriteAllText("origin_graph.svg", svgOriginGraph, Encoding.UTF8);
#endif

        // ищем короткие пути между всеми парами POI
        var shortPathPoint = new List<GeomPoint>(originPoints.Count);
        var shortEdges = new HashSet<IEdge<GeomPoint>>(originEdges.Count);
        var maxLenPath = 0;
        var longPairs = new HashSet<(GeomPoint, GeomPoint)>();

        var validPoi2 = originPoints.Where(p => p.IsPoi).ToList();
        foreach (var pair in PointPairsHelper.GetUniquePairs(validPoi2))
        {
            var shortPath = QuickPathFinder
                .FindPath(originEdges, originPoints,pair.Item1, pair.Item2)
                .ToList();
            maxLenPath = Math.Max(maxLenPath, shortPath.Count);
            shortPathPoint.AddRange(shortPath);
            foreach (IEdge<GeomPoint> edge in PointPairsHelper.GetEdges(shortPath))
            {
                shortEdges.Add(edge);
            }

            if (shortPath.Any(p => !p.IsPoi))
            {
                longPairs.Add((pair.Item1, pair.Item2));
                longPairs.Add((pair.Item2, pair.Item1));
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
        var recoveredPoints = shortPathPoint
            .Concat(originPoints.Where(p => !urbanFilter.Skip(p.AsVector2())))
            .Select(p => p.AsVector2())
            .ToList();
        
        // Строим воронова по стабильным точкам и коротким путям
        var voronator2 = new Voronator(recoveredPoints);
        var (originPoints2, originEdges2) = VoronatorToQuickGraphAdapter.ConvertToQuickGraph(poiMaxId + 1,polygonMap, voronator2, settings.HexSize);

#if DEBUG
        // рисуем воронова по стабильным точкам и коротким путям
        var svgVoronRecovered = GenerateSvg.Generate(polygonMap, originPoints2, originEdges2);
        File.WriteAllText("voron_recovered.svg", svgVoronRecovered, Encoding.UTF8);
#endif

        // рисуем финальный граф
        // var svgFilteredGraph = GenerateSvg.Generate(polygonMap, points, edges);
        // File.WriteAllText("filtered_graph.svg", svgFilteredGraph, Encoding.UTF8);

        return (originEdges2, longPairs, maxLenPath);
    }
}