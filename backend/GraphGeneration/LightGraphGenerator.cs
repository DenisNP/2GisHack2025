using System.Text;
using GraphGeneration.A;
using GraphGeneration.Filters;
using GraphGeneration.Geometry;
using GraphGeneration.Models;
using GraphGeneration.Svg;
using NetTopologySuite.Geometries;
using PathScape.Domain.Models;
using VoronatorSharp;

namespace GraphGeneration;

public static class LightGraphGenerator
{
    public static GeomPoint[] GenerateEdges(List<ZonePolygon> polygons, List<GeomPoint> poi)
    {
        PolygonMap polygonMap = PolygonHelper.GetPolygonMap(polygons);
        double avArea = polygonMap.Available.Sum(p => p.Area);
        double side = Math.Sqrt(avArea);
        Console.WriteLine("Total Area: " + avArea + "; side: " + side);
        float hexSize = Math.Clamp((float)side / 200f, 0.5f, 3f);
        float bigHexSize = Math.Clamp(hexSize * 3, 3f, 5f);

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
        List<Vector2> centersUrban = polygonMap
            .Urban
            .Where(u => polygonMap.Restricted.Any(u.Intersects)) 
            .SelectMany(u => HexagonalGridGenerator.GenerateHexagonalGridInPolygon(poiMaxId, new ZonePolygon(u, ZoneType.Urban), bigHexSize))
            .ToList();
        poiMaxId += centersUrban.Count;
        List<Vector2> generatedHexPoints = HexagonalMultiPolygonGenerator.GenerateHexagonalPoints(poiMaxId, polygonMap, settings);
        poiMaxId += generatedHexPoints.Count;
        List<Vector2> generatedBigHexPoints = HexagonalMultiPolygonGenerator.GenerateSpacedHexagonalPointsOutside(poiMaxId, polygonMap, bigHexSize);
        poiMaxId += generatedBigHexPoints.Count;

        // Создаем общую диаграмму Вороного/Делоне для всех точек
        var voronator = new Voronator(generatedHexPoints.Concat(validPoi.Select(x => x.AsVector2())).Concat(centersUrban).Concat(generatedBigHexPoints).ToArray());

        // Строим граф для а*
        var (originPoints, originEdges) = VoronatorToQuickGraphAdapter.ConvertToQuickGraph(polygonMap, voronator, settings.HexSize);
        
#if DEBUG
        // рисуем исходный граф
        var svgOriginGraph = GenerateSvg.Generate(polygonMap, originPoints, originEdges);
        File.WriteAllText("origin_graph.svg", svgOriginGraph, Encoding.UTF8);
#endif
        
        // считаем кратчайшие расстояния
        var pairs = GeneratePoiPairs(originPoints.Where(p => p.IsPoi).ToList(), originEdges, polygonMap, out int uniqCount).ToList();
        //int iterations = 2;
        Console.WriteLine("Pairs: " + pairs.Count);

        var pointsByPairs = new Dictionary<int, List<GeomPoint>>();

        //while (iterations-- > 0)
        //{
            foreach ((GeomPoint, GeomPoint) pair in pairs)
            {
                var pairId = GetPairId(pair);
                
                List<GeomPoint> shortPath = QuickPathFinder
                    .FindPath(originEdges, originPoints, pair.Item1, pair.Item2)
                    .ToList();

                // Прибавляем всем точкам пути влияния
                shortPath.ForEach(p =>
                {
                    /*if (p.Influence < 3)
                    {
                        p.Influence += 1;
                    }*/
                    //p.AddPath(pair.Item1, pair.Item2);
                    p.Influence++;
                    if (!pointsByPairs.ContainsKey(pairId))
                    {
                        pointsByPairs.Add(pairId, []);
                    }
                    pointsByPairs[pairId].Add(p);
                    /*var neighbors = GetNeighbours(p, originEdges);
                    foreach (GeomPoint np in neighbors)
                    {
                        np.Influence += 0.4;
                    }*/
                });
            }
            //var totalPairs = pairs.Count;
            //var coeff = 0.8f * Math.Floor(totalPairs / (uniqCount * 1f));
            for (int inc = 0; inc <= 20; inc += (inc / 10 + 1))
            {
                int pathsCount = 0;
                foreach (KeyValuePair<int, List<GeomPoint>> pair in pointsByPairs)
                {
                    var count = pair.Value.Count(p => p.Influence > inc);
                    bool show = count > 0.65 * pair.Value.Count;
                    if (show)
                    {
                        pair.Value.ForEach(p => p.Show = true);
                        pathsCount++;
                    }
                }

                Console.WriteLine("Increment: " + inc + "; paths: " + pathsCount);
                if (inc == 0 && pathsCount > 10)
                {
                    originPoints.ForEach(p => p.Show = false);
                    continue;
                }

                if (pathsCount <= Math.Sqrt(pairs.Count) * 10)
                {
                    break;
                }
                else
                {
                    originPoints.ForEach(p => p.Show = false);
                }
            }

/*#if DEBUG

            Console.WriteLine(iterations);
            // рисуем исходный граф
            var svgShortGraph1 = GenerateSvg.Generate(polygonMap, originPoints.ToList(), originEdges.ToList());
            File.WriteAllText("short_graph"+iterations+".svg", svgShortGraph1, Encoding.UTF8);

#endif  */
        //}
        
#if DEBUG
        // рисуем исходный граф
        var svgShortGraph = GenerateSvg.Generate(polygonMap, originPoints.ToList(), originEdges.ToList());
        File.WriteAllText("short_graph.svg", svgShortGraph, Encoding.UTF8);
#endif        
        
        var pointAllowedFilter = new PointAllowedFilter(polygonMap.Available);
        return originPoints.Where(e => e.Show && !e.IsPoi && !pointAllowedFilter.Skip(e.AsVector2())).ToArray();
    }

    private static int GetPairId((GeomPoint, GeomPoint) pair)
    {
        var minp = pair.Item1.Id < pair.Item2.Id ? pair.Item1.Id : pair.Item2.Id;
        var maxp = minp == pair.Item1.Id ? pair.Item2.Id : pair.Item1.Id;

        var combined = minp * 10000 + maxp;
        return combined;
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

    private static IEnumerable<(GeomPoint, GeomPoint)> GeneratePoiPairs(List<GeomPoint> pois, List<GeomEdge> edges, PolygonMap polygonMap, out int uniqCount)
    {
        var pairs = GenerateUniqPairs(pois).ToList();
        pairs.RemoveAll(p =>
        {
            var n = GetNeighbours(p.Item1, edges);
            return n.Any(x => x.Id == p.Item2.Id);
        });
        pairs.RemoveAll(p =>
        {
            var line = new LineString([new Coordinate(p.Item1.X, p.Item1.Y),  new Coordinate(p.Item2.X, p.Item2.Y)]);
            foreach (Polygon polygon in polygonMap.Available)
            {
                if (line.Crosses(polygon))
                {
                    return false;
                }
            }
            return true;
        });

        uniqCount = pairs.Count;
        return pairs;

        var minWeight = pairs.Min(GetPairWeight);
        var coeff = 1 / minWeight;
        var counts = pairs.Select(p => (count: GetPairWeight(p) * coeff, pair: p)).ToList();
        
        var allPairs = new List<(GeomPoint, GeomPoint)>();
        foreach (var pair in counts)
        {
            for (var i = 0; i < pair.count; i++)
            {
                allPairs.Add(pair.pair);
            }
        }
        
        return allPairs;
    }

    private static double GetPairWeight((GeomPoint, GeomPoint) pair)
    {
        return (pair.Item1.Weight + pair.Item2.Weight) / 2;
    }

    private static IEnumerable<GeomPoint> GetNeighbours(GeomPoint point, IEnumerable<GeomEdge> edges)
    {
        return edges.Where(e => e.From.Id == point.Id || e.To.Id == e.Id)
            .Select(e => e.From.Id == point.Id ? e.To : e.From);
    }
}