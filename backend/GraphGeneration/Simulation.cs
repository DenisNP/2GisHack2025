using System.Text;
using GraphGeneration.Geometry;
using GraphGeneration.Models;
using GraphGeneration.Svg;

namespace GraphGeneration;

public static class Simulation
{
    private const int walkers = 300;
    private const int influenceDepth = 1;
    private static double _influenceIncrement = 0.1;

    public static void Run(
        IList<GeomPoint> points,
        List<GeomPoint> pois,
        List<(GeomPoint, GeomPoint)> pairs,
        Dictionary<(int, int), List<GeomPoint>> paths,
        Dictionary<int, List<(GeomPoint neighbor, double cost)>> neighbors,
        PolygonMap polygonMap,
        double hexSize
    )
    {
        _influenceIncrement *= hexSize;

        var orderedPairs = pairs.OrderByDescending(pp => 1000000 * (pp.Item1.Weight + pp.Item2.Weight) + paths[(pp.Item1.Id, pp.Item2.Id)].Count).ToList();
        var sumWeight = pairs.Sum(pp => pp.Item1.Weight + pp.Item2.Weight);

        double walkersOnPair = walkers / sumWeight;
        int pn = 0;

        while (orderedPairs.Count > 0)
        {
            var pair = orderedPairs.First();
            orderedPairs.RemoveAt(0);

            var pairWeight = pair.Item1.Weight + pair.Item2.Weight;
            var pairWalkers = (int)Math.Round(pairWeight * walkersOnPair);
            var path = paths[(pair.Item1.Id, pair.Item2.Id)];
            
            Console.WriteLine("Walkers: " + pairWalkers + "; pairs left: " + orderedPairs.Count);

            HashSet<GeomPoint>? lastPath = null;
            lastPath = Go(pair.Item1, pair.Item2, path, neighbors, pairWalkers); 

            pn++;

            if (pn % 10 == 0)
            {
                var svgPathsGraph = GenerateSvg.Generate(
                    polygonMap,
                    points.ToList(),
                    [],
                    lastPath
                );
                File.WriteAllText("paths_graph_"+pn+".svg", svgPathsGraph, Encoding.UTF8);
            }

            // Нормируем
            /*var maxInfluence = points.Max(n => n.Influence);
            foreach (GeomPoint point in points)
            {
                point.Influence /= maxInfluence;
            }*/
        }
    }

    private static HashSet<GeomPoint> Go(
        GeomPoint start,
        GeomPoint finish,
        List<GeomPoint> path,
        Dictionary<int, List<(GeomPoint neighbor, double cost)>> neighbors,
        int walkers
    )
    {
        if (path.Count == 0)
        {
            return [];
        }
        
        // точка, где мы сейчас
        GeomPoint currentPoint = start;
        
        // запомним следующую желаемую точку пути, ближе всего к которой мы находиимся
        var nextPathPointIndex = 1;
        GeomPoint nextPathPoint = path[1];
        HashSet<GeomPoint> visited = [currentPoint];

        while (currentPoint.Id != finish.Id)
        {
            /*
             *  Нужно выбрать следующую точку, куда двигаться, из таких кандидатов:
             *  1. Обычная следующая точка самого короткого пути
             *  2. Среди ближайших соседей точка с наибольшим влиянием, если такая есть
             *  3. Среди ближайших соседей точка, максимально близкая к текущему маршруту
             */
            GeomPoint? nextPoint = null;

            // Ближайшие соседи
            List<(GeomPoint neighbor, int depth)> currentNeighbors = GetAllNeighbors(currentPoint, neighbors, 1)
                .Where(n => !visited.Contains(n.neighbor))
                .ToList();

            if (currentNeighbors.Count == 0)
            {
                // Соседей нет, просто перемещаемся по пути
                nextPoint = nextPathPoint;
                
                // Если мы не в конце пути, то метим себе следующую точку на пути в качестве цели на новый шаг
                if (nextPoint.Id != finish.Id)
                {
                    nextPathPointIndex++;
                    nextPathPoint = path[nextPathPointIndex];
                }
            }
            else
            {
                // Лучший сосед
                var best = currentNeighbors.MaxBy(n => MeasurePointAttractiveness(n.neighbor, nextPathPoint));

                // Выбираем куда сделаем шаг: по влиянию или в сторону пути
                nextPoint = best.neighbor;
                
                // нужно спроецировать точку реального пути на оригинальный путь, чтобы отсчитывать движение вдоль пути
                GeomPoint? closestNextPathPoint = path.Skip(nextPathPointIndex).MinBy(pp => pp.DistanceTo(nextPoint));
                if (closestNextPathPoint != null)
                {
                    nextPathPoint = GetNextPointOnPath(path, closestNextPathPoint, nextPathPoint, out nextPathPointIndex);
                }
                else if (nextPoint.Id != finish.Id)
                {
                    throw new InvalidOperationException();
                }
            }

            // Выбрали следующую точку, теперь пойдём по ней
            currentPoint = nextPoint;
            visited.Add(currentPoint);
        }

        // Оставляем вытоптанный след
        HashSet<GeomPoint> incremented = new();
        foreach (GeomPoint point in visited)
        {
            if (incremented.Add(point))
            {
                point.Influence += _influenceIncrement * walkers;
            }

            var influencedNeighbors = GetAllNeighbors(point, neighbors, influenceDepth);
            influencedNeighbors.ForEach(n =>
            {
                if (incremented.Add(n.neighbor))
                {
                    n.neighbor.Influence += _influenceIncrement * walkers / n.depth;
                }
            });
        }

        _influenceIncrement *= Math.Pow(0.7, walkers);
        return visited;
    }

    private static double MeasurePointAttractiveness(GeomPoint point, GeomPoint nextPathPoint)
    {
        double influence = point.Influence;
        double distance = point.DistanceTo(nextPathPoint);

        return Math.Pow(influence, 2) - distance;
    }

    /// <summary>
    /// Получает всех соседей заданной точки на глубинах от 1 до n включительно
    /// </summary>
    /// <param name="point">Исходная точка</param>
    /// <param name="neighbors">Словарь соседей для всех точек</param>
    /// <param name="maxDepth">Максимальная глубина поиска (n = 1 для прямых соседей, n = 2 для соседей и соседей соседей и т.д.)</param>
    /// <returns>Список всех соседних точек с их уровнями глубины на глубинах от 1 до maxDepth</returns>
    private static List<(GeomPoint neighbor, int depth)> GetAllNeighbors(
        GeomPoint point, 
        Dictionary<int, List<(GeomPoint neighbor, double cost)>> neighbors, 
        int maxDepth)
    {
        if (maxDepth <= 0)
        {
            return new List<(GeomPoint neighbor, int depth)>();
        }

        var result = new List<(GeomPoint neighbor, int depth)>();
        var visited = new HashSet<int> { point.Id };
        var queue = new Queue<(GeomPoint currentPoint, int currentDepth)>();

        // Добавляем исходную точку в очередь с глубиной 0
        queue.Enqueue((point, 0));

        while (queue.Count > 0)
        {
            (GeomPoint currentPoint, int currentDepth) = queue.Dequeue();

            // Если достигли нужной глубины, добавляем в результат
            if (currentDepth > 0 && currentDepth <= maxDepth)
            {
                result.Add((currentPoint, currentDepth));
            }

            // Если глубина меньше максимальной, продолжаем поиск
            if (currentDepth < maxDepth && neighbors.TryGetValue(currentPoint.Id, out var currentNeighbors))
            {
                foreach ((GeomPoint neighbor, _) in currentNeighbors)
                {
                    if (visited.Add(neighbor.Id))
                    {
                        queue.Enqueue((neighbor, currentDepth + 1));
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Находит ближайшую точку из переданного пути среди соседей до заданной глубины
    /// </summary>
    /// <param name="currentPoint">Текущая точка</param>
    /// <param name="path">Путь для поиска ближайшей точки</param>
    /// <param name="neighbors">Словарь соседей для всех точек</param>
    /// <param name="maxDepth">Максимальная глубина поиска среди соседей</param>
    /// <returns>Кортеж с ближайшей точкой и расстоянием, или null если точка не найдена</returns>
    public static (GeomPoint closestPoint, double distance)? GetClosestPathPoint(
        GeomPoint currentPoint,
        IEnumerable<GeomPoint> path,
        Dictionary<int, List<(GeomPoint neighbor, double cost)>> neighbors,
        int maxDepth)
    {
        // Создаем HashSet для быстрого поиска точек пути
        var pathPoints = new HashSet<int>(path.Select(p => p.Id));

        if (pathPoints.Count == 0)
        {
            return null;
        }

        // Получаем всех соседей с их глубинами
        var allNeighbors = GetAllNeighbors(currentPoint, neighbors, maxDepth);

        (GeomPoint closestPoint, double distance)? result = null;
        double minDistance = double.MaxValue;

        foreach (var (neighbor, depth) in allNeighbors)
        {
            // Проверяем, есть ли эта точка в пути
            if (pathPoints.Contains(neighbor.Id))
            {
                // Вычисляем расстояние между текущей точкой и соседом
                double distance = currentPoint.DistanceTo(neighbor);

                // Если это ближайшая точка, обновляем результат
                if (distance < minDistance)
                {
                    minDistance = distance;
                    result = (neighbor, distance);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Определяет следующую точку на пути относительно опорной точки
    /// </summary>
    /// <param name="path">Путь для анализа</param>
    /// <param name="currentPoint">Текущая точка на пути</param>
    /// <param name="referencePoint">Опорная точка</param>
    /// <param name="nextPathIndex">Индекс новой выбранной точки</param>
    /// <returns>Текущую точку если она дальше опорной, иначе следующую после опорной</returns>
    public static GeomPoint GetNextPointOnPath(
        List<GeomPoint> path,
        GeomPoint currentPoint,
        GeomPoint referencePoint,
        out int nextPathIndex)
    {
        if (path == null || path.Count == 0)
        {
            throw new ArgumentException("Путь не может быть пустым", nameof(path));
        }

        if (currentPoint == null)
        {
            throw new ArgumentNullException(nameof(currentPoint));
        }

        int referenceIndex = path.IndexOf(referencePoint);

        // Находим индекс текущей точки в пути
        int currentIndex = -1;
        for (int i = 0; i < path.Count; i++)
        {
            if (path[i].Id == currentPoint.Id)
            {
                currentIndex = i;
                break;
            }
        }

        // Проверяем, что текущая точка найдена в пути
        if (currentIndex == -1)
        {
            throw new ArgumentException("Текущая точка не найдена в пути", nameof(currentPoint));
        }

        // Если текущая точка дальше опорной по пути, возвращаем текущую
        if (currentIndex > referenceIndex)
        {
            nextPathIndex = referenceIndex;
            return currentPoint;
        }

        // Иначе возвращаем следующую после опорной
        int nextIndex = referenceIndex + 1;
        
        // Если опорная точка - последняя в пути, возвращаем её
        if (nextIndex >= path.Count)
        {
            nextPathIndex = referenceIndex;
            return path[referenceIndex];
        }

        nextPathIndex = nextIndex;
        return path[nextIndex];
    }

    /// <summary>
    /// Выбирает случайный элемент из списка с учётом весов (более весомые элементы выбираются чаще)
    /// </summary>
    /// <typeparam name="T">Тип элементов в списке</typeparam>
    /// <param name="items">Список элементов для выбора</param>
    /// <param name="weightSelector">Функция для получения веса элемента</param>
    /// <returns>Случайно выбранный элемент с учётом весов</returns>
    /// <exception cref="ArgumentException">Выбрасывается если список пуст или все веса неположительные</exception>
    private static T SelectWeightedRandom<T>(
        IList<T> items, 
        Func<T, double> weightSelector)
    {
        if (items == null || items.Count == 0)
        {
            throw new ArgumentException("Список не может быть пустым", nameof(items));
        }

        if (weightSelector == null)
        {
            throw new ArgumentNullException(nameof(weightSelector));
        }

        // Вычисляем общий вес всех элементов
        double totalWeight = 0;
        var weights = new double[items.Count];
        
        for (int i = 0; i < items.Count; i++)
        {
            double weight = weightSelector(items[i]);
            if (weight < 0)
            {
                throw new ArgumentException($"Вес элемента {i} не может быть отрицательным: {weight}");
            }
            weights[i] = weight;
            totalWeight += weight;
        }

        if (totalWeight <= 0)
        {
            throw new ArgumentException("Общий вес всех элементов должен быть положительным");
        }

        // Генерируем случайное число от 0 до общего веса
        double randomValue = Random.Shared.NextDouble() * totalWeight;

        // Находим элемент, соответствующий случайному числу
        double currentWeight = 0;
        for (int i = 0; i < items.Count; i++)
        {
            currentWeight += weights[i];
            if (randomValue <= currentWeight)
            {
                return items[i];
            }
        }

        // Этот код не должен выполняться, но на всякий случай возвращаем последний элемент
        return items[^1];
    }
}