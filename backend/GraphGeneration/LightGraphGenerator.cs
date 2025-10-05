using System.Text;
using AntAlgorithm;
using GraphGeneration.A;
using GraphGeneration.Filters;
using GraphGeneration.Geometry;
using GraphGeneration.Models;
using QuickGraph;
using VoronatorSharp;

namespace GraphGeneration;

public class LightGraphGenerator
{
    public static GeomPoint[] GenerateEdges(List<ZonePolygon> polygons, List<GeomPoint> poi)
    {
        // Настройки гексагонального заполнения
        var settings = new HexagonalMultiPolygonGenerator.HexagonalSettings
        {
            HexSize = 0.5f,
            Density = 1,
            UseConvexHull = false,
            AddPolygonVertices = false,
            AddEdgePoints = false,
            EdgePointSpacing = 2f
        };

        PolygonMap polygonMap = PolygonHelper.GetPolygonMap(polygons);
        
        var poiFilter = new PointAllowedFilter(polygonMap.Render);
        List<GeomPoint> validPoi = poi.Where(p => !poiFilter.Skip(p.AsVector2())).ToList();
        int poiMaxId = validPoi.Max(p => p.Id);

        // Генерируем точки
        List<Vector2> centersUrban = polygonMap
            .Urban
            .Where(u => polygonMap.Restricted.Any(u.Intersects)) 
            .SelectMany(u => HexagonalGridGenerator.GenerateHexagonalGridInPolygon(poiMaxId, new ZonePolygon(u, ZoneType.Urban), 4))
            .ToList();
        poiMaxId += centersUrban.Count;
        List<Vector2> generatedHexPoints = HexagonalMultiPolygonGenerator.GenerateHexagonalPoints(poiMaxId, polygonMap, settings);

        // Создаем общую диаграмму Вороного/Делоне для всех точек
        var voronator = new Voronator(generatedHexPoints.Concat(validPoi.Select(x => x.AsVector2())).Concat(centersUrban).ToArray());

        // Строим граф для а*
        Dictionary<Vector2, GeomPoint> pointsByLocation = poi.ToDictionary(p => p.AsVector2());

        AdjacencyGraph<Vector2, Edge<Vector2>> graph = VoronatorToQuickGraphAdapter.ConvertToQuickGraph(polygonMap, voronator, settings.HexSize);
        int id = poiMaxId + 1;
        List<GeomPoint> originPoints = graph.Vertices.Select(v =>
        {
            if (!pointsByLocation.TryGetValue(v, out GeomPoint? retP))
            {
                var p = new GeomPoint
                {
                    Id = id++,
                    X = v.X,
                    Y = v.Y,
                    Weight = 0,
                    //Influence = 0
                };
                pointsByLocation.Add(v, p);
                return p;
            }
            else
            {
                return retP;
            }
        }).ToList();
        List<GeomEdge> originEdges = graph.Edges.Select(e => new GeomEdge
        {
            From = pointsByLocation[e.Source],
            To = pointsByLocation[e.Target],
            Weight = 0
        }).ToList();
        
#if DEBUG
        // рисуем исходный граф
        var svgOriginGraph = GenerateSvg.Generate(polygonMap, originPoints, originEdges);
        File.WriteAllText("origin_graph.svg", svgOriginGraph, Encoding.UTF8);
#endif
        
        // считаем кратчайшие расстояния
        //var graph2 = ConvertToQuickGraph(originEdges, originPoints);
        var pairs = GeneratePoiPairs(originPoints.Where(p => p.IsPoi).ToList(), originEdges, out int uniqCount).ToList();
        //int iterations = 2;

        var pointsByPairs = new Dictionary<int, List<GeomPoint>>();

        //while (iterations-- > 0)
        //{
            foreach ((GeomPoint, GeomPoint) pair in pairs)
            {
                if (GetNeighbours(pair.Item1, originEdges).Any(n => n.Id == pair.Item2.Id))
                {
                    continue;
                }

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
            //var coeff = Math.Floor(totalPairs / (uniqCount * 1f));

            foreach (KeyValuePair<int, List<GeomPoint>> pair in pointsByPairs)
            {
                var count = pair.Value.Count(p => p.Influence > 1);
                if (count > 0.65 * pair.Value.Count)
                {
                    pair.Value.ForEach(p => p.Show = true);
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

        return originPoints.Where(e => e.Show).ToArray();
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

    private static IEnumerable<(GeomPoint, GeomPoint)> GeneratePoiPairs(List<GeomPoint> pois, List<GeomEdge> edges, out int uniqCount)
    {
        var pairs = GenerateUniqPairs(pois).ToList();
        pairs.RemoveAll(p =>
        {
            var n = GetNeighbours(p.Item1, edges);
            return n.Any(x => x.Id == p.Item2.Id);
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
    
    public static AdjacencyGraph<GeomPoint, GeomEdge> ConvertToQuickGraph(List<GeomEdge> originEdges, List<GeomPoint> originPoints)
    {
        var graph = new AdjacencyGraph<GeomPoint, GeomEdge>();
        originPoints.ForEach(p => graph.AddVertex(p));
        originEdges.ForEach(e => graph.AddEdge(e));
        
        return graph;
    }
}