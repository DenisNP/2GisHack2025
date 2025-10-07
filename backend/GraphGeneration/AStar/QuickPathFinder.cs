using GraphGeneration.Models;
using VoronatorSharp;

namespace GraphGeneration.AStar;

public static class QuickPathFinder
{
    public static IEnumerable<GeomPoint> FindPath(IList<GeomPoint> points, Dictionary<int, List<(GeomPoint neighbor, double cost)>> neighbors, GeomPoint start, GeomPoint end)
    {
        // Эвристическая функция (евклидово расстояние)
        double Heuristic(GeomPoint a, GeomPoint b)
        {
            return Vector2.Distance(a.AsVector2(), b.AsVector2());
        }
        
        // Открытый список (приоритетная очередь)
        var openSet = new PriorityQueue<GeomPoint, double>();
        openSet.Enqueue(start, 0);
        
        // Откуда пришли к каждому узлу
        var cameFrom = new Dictionary<int, GeomPoint>();
        
        // g-score: стоимость пути от старта до узла
        var gScore = new Dictionary<int, double>();
        foreach (var point in points)
        {
            gScore[point.Id] = double.PositiveInfinity;
        }
        gScore[start.Id] = 0;
        
        // f-score: g-score + эвристика
        var fScore = new Dictionary<int, double>();
        foreach (var point in points)
        {
            fScore[point.Id] = double.PositiveInfinity;
        }
        fScore[start.Id] = Heuristic(start, end);
        
        // Множество узлов в открытом списке (для быстрой проверки)
        var openSetHash = new HashSet<int> { start.Id };
        
        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();
            openSetHash.Remove(current.Id);
            
            // Достигли цели
            if (current.Id == end.Id)
            {
                return ReconstructPath(cameFrom, current);
            }
            
            // Проверяем всех соседей
            if (!neighbors.ContainsKey(current.Id))
                continue;
                
            foreach (var (neighbor, cost) in neighbors[current.Id])
            {
                double tentativeGScore = gScore[current.Id] + cost;
                
                if (tentativeGScore < gScore[neighbor.Id])
                {
                    // Этот путь лучше
                    cameFrom[neighbor.Id] = current;
                    gScore[neighbor.Id] = tentativeGScore;
                    fScore[neighbor.Id] = tentativeGScore + Heuristic(neighbor, end);
                    
                    if (!openSetHash.Contains(neighbor.Id))
                    {
                        openSet.Enqueue(neighbor, fScore[neighbor.Id]);
                        openSetHash.Add(neighbor.Id);
                    }
                }
            }
        }
        
        // Путь не найден
        return [];
    }
    
    private static List<GeomPoint> ReconstructPath(Dictionary<int, GeomPoint> cameFrom, GeomPoint current)
    {
        var path = new List<GeomPoint> { current };
        
        while (cameFrom.ContainsKey(current.Id))
        {
            current = cameFrom[current.Id];
            path.Insert(0, current);
        }
        
        return path;
    }
}