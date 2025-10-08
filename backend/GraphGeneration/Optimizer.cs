using System.Text;
using GraphGeneration.AStar;
using GraphGeneration.Geometry;
using GraphGeneration.Models;
using GraphGeneration.Svg;

namespace GraphGeneration;

public static class Optimizer
{
    private const int influenceDepth = 1;
    private static double _influenceIncrement = 0.25;

    public static void Run(
        List<GeomPoint> points,
        List<GeomPoint> pois,
        List<(GeomPoint, GeomPoint)> pairs,
        Dictionary<int, List<(GeomPoint neighbor, double cost)>> neighbors,
        PolygonMap polygonMap,
        double hexSize
    )
    {
        _influenceIncrement *= hexSize;

        List<(GeomPoint, GeomPoint)> orderedPairs = pairs
            .OrderByDescending(pp => 1000000 * (pp.Item1.Weight + pp.Item2.Weight) + pp.Item1.DistanceTo(pp.Item2))
            .ToList();

        double maxWeight = pairs.Max(pp => pp.Item1.Weight + pp.Item2.Weight);
        //int pn = 0;

        while (orderedPairs.Count > 0)
        {
            (GeomPoint, GeomPoint) pair = orderedPairs.First();
            orderedPairs.RemoveAt(0);

            double pairNormalizedWeight = (pair.Item1.Weight + pair.Item2.Weight) / maxWeight;
            Console.Write(orderedPairs.Count + " ");

            // Находим кратчайший пусть между самыми популярными точками
            List<GeomPoint> lastPath = QuickPathFinder.FindPath(points, neighbors, pair.Item1, pair.Item2); 
            
            // Увеличиваем влияние точек в зависимости от числа ходящих
            HashSet<GeomPoint> processed = new();
            lastPath.ForEach(p =>
            {
                if (processed.Add(p))
                {
                    p.Influence += _influenceIncrement * pairNormalizedWeight;
                }

                foreach ((GeomPoint neighbor, int depth) in Simulation.GetAllNeighbors(p, neighbors, influenceDepth))
                {
                    if (processed.Add(neighbor))
                    {
                        neighbor.Influence += _influenceIncrement * pairNormalizedWeight / Math.Pow(1 + depth, 2);
                    }
                }
            });

            // Пересчитываем стоимость рёбер
            foreach (GeomPoint geomPoint in processed)
            {
                List<(GeomPoint neighbor, double cost)> currentNeighbours = neighbors[geomPoint.Id];
                for (var i = 0; i < currentNeighbours.Count; i++)
                {
                    GeomPoint n = currentNeighbours[i].neighbor;
                    double distance = geomPoint.DistanceTo(n);
                    double cost = distance / (1 + geomPoint.Influence + n.Influence);
                    currentNeighbours[i] = (n, cost);
                }
            }
#if DEBUG
            /*pn++;
            if (pn % 10 == 0)
            {
                var svgPathsGraph = GenerateSvg.Generate(
                    polygonMap,
                    points.ToList(),
                    [],
                    lastPath.ToHashSet()
                );
                File.WriteAllText("paths_graph_"+pn+".svg", svgPathsGraph, Encoding.UTF8);
            }*/
#endif
        }

        Console.WriteLine();
    }
}