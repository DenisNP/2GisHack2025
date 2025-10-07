using GraphGeneration.Models;

namespace GraphGeneration.Geometry;

public static class GeomHelper
{
    public static GeomPoint GetMainClusterPoint(List<GeomPoint> cluster)
    {
        if (cluster == null || cluster.Count == 0)
            throw new ArgumentException("Кластер не может быть пустым", nameof(cluster));

        if (cluster.Count == 1)
            return cluster[0];

        // Вычисляем средние координаты (центр кластера)
        double sumX = cluster.Sum(p => p.X);
        double sumY = cluster.Sum(p => p.Y);
        double centerX = sumX / cluster.Count;
        double centerY = sumY / cluster.Count;

        // Находим ближайшую к центру точку
        GeomPoint closestPoint = cluster[0];
        double minDistance = CalculateDistance(closestPoint, new GeomPoint(0, (float)centerX, (float)centerY, 0));

        for (int i = 1; i < cluster.Count; i++)
        {
            double distance = CalculateDistance(cluster[i], new GeomPoint(0, (float)centerX, (float)centerY, 0));
            if (distance < minDistance)
            {
                minDistance = distance;
                closestPoint = cluster[i];
            }
        }

        // Суммируем веса всех точек в кластере
        double totalWeight = cluster.Sum(p => p.Weight);

        // Обновляем вес ближайшей точки
        closestPoint.Weight = totalWeight;

        // Возвращаем ту же точку с обновленным весом
        return closestPoint;
    }
    
    public static List<List<GeomPoint>> Clusterize(IList<GeomPoint> points, PolygonMap map, double maxClusterDistance)
    {
        if (points == null || points.Count == 0)
            return new List<List<GeomPoint>>();

        var clusters = new List<List<GeomPoint>>();
        var usedPoints = new HashSet<int>();

        for (int i = 0; i < points.Count; i++)
        {
            if (usedPoints.Contains(points[i].Id))
                continue;

            var currentCluster = new List<GeomPoint> { points[i] };
            usedPoints.Add(points[i].Id);

            // Ищем все точки, которые можно добавить в текущий кластер
            for (int j = i + 1; j < points.Count; j++)
            {
                if (usedPoints.Contains(points[j].Id))
                    continue;

                // Проверяем, можно ли добавить эту точку в кластер
                if (CanAddToCluster(points[j], currentCluster, map, maxClusterDistance))
                {
                    currentCluster.Add(points[j]);
                    usedPoints.Add(points[j].Id);
                }
            }

            clusters.Add(currentCluster);
        }

        return clusters;
    }

    private static bool CanAddToCluster(GeomPoint candidate, List<GeomPoint> cluster, PolygonMap map, double maxClusterDistance)
    {
        // Проверяем расстояние до всех точек в кластере
        foreach (var point in cluster)
        {
            var distance = CalculateDistance(candidate, point);
            if (distance > maxClusterDistance)
            {
                return false;
            }

            // Проверяем, не пересекает ли линия между точками доступные полигоны
            if (PolygonHelper.IsPairCrossesAvailable(candidate, point, map)
                || PolygonHelper.IsPairCrossesRestricted(candidate, point, map))
            {
                return false;
            }
        }

        return true;
    }

    private static double CalculateDistance(GeomPoint p1, GeomPoint p2)
    {
        var dx = p1.X - p2.X;
        var dy = p1.Y - p2.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}