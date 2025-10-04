

using NetTopologySuite.Geometries;
using VoronatorSharp;

namespace GraphGeneration.A;

public class VoronoiEdgeFinder
{
    private Voronator _voronator;
    private List<Vector2> _allPoints;
    
    public VoronoiEdgeFinder(Voronator voronator, List<Vector2> allPoints)
    {
        _voronator = voronator;
        _allPoints = allPoints;
    }
    
    public class NeighborhoodResult
    {
        public Vector2 Point { get; set; }
        public List<Vector2> NeighborPoints { get; set; } = new List<Vector2>();
        public List<Edge> Edges { get; set; } = new List<Edge>();
    }
    
    public class Edge
    {
        public Vector2 Source { get; set; }
        public Vector2 Target { get; set; }
        
        public override string ToString() => $"{Source} -> {Target}";
        
        // Для сравнения рёбер (независимо от направления)
        public override bool Equals(object obj)
        {
            if (obj is Edge other)
            {
                return (Source.Equals(other.Source) && Target.Equals(other.Target)) ||
                       (Source.Equals(other.Target) && Target.Equals(other.Source));
            }
            return false;
        }
        
        public override int GetHashCode()
        {
            return Source.GetHashCode() ^ Target.GetHashCode();
        }
    }
    
    public Dictionary<Vector2, NeighborhoodResult> FindNeighborsWithEdges(List<NetTopologySuite.Geometries.Polygon> ignore, float hexSize, List<Vector2> partialPoints)
    {
        var result = new Dictionary<Vector2, NeighborhoodResult>();
        
        foreach (var point in partialPoints)
        {
            result[point] = FindNeighborsWithEdges(ignore, hexSize,point);
        }
        
        return result;
    }
    
    public NeighborhoodResult FindNeighborsWithEdges(List<NetTopologySuite.Geometries.Polygon> ignore, float hexSize, Vector2 point)
    {
        var result = new NeighborhoodResult { Point = point };
        var neighborSet = new HashSet<Vector2>();
        var edgeSet = new HashSet<Edge>();
        
        int pointIndex = _voronator.Find(point);
        if (pointIndex < 0) return result;
        
        try
        {
            // Получаем соседние точки
            var neighborIndices = _voronator.Neighbors(pointIndex);
            var sr = HexagonalGridGenerator.CalculateExpectedHexDistance(hexSize);
            
            foreach (int neighborIndex in neighborIndices)
            {
                // if (neighborIndex >= 0 && neighborIndex < _allPoints.Count)
                {
                    var neighborPoint = _voronator.Delaunator.Points[neighborIndex];

                    neighborSet.Add(neighborPoint);
                    
                    if (sr * 2 < Vector2.Distance(point, neighborPoint))
                    {
                        continue;
                    }
                    
                    // Создаем геометрическое представление ребра
                    var lineString = new LineString([
                        new Coordinate(point.x, point.y),
                        new Coordinate(neighborPoint.x, neighborPoint.y)
                    ]);

                    // Проверяем, пересекает ли ребро любой из игнорируемых полигонов
                    var intersectsIgnoredPolygon = false;
                    foreach (var polygon in ignore)
                    {
                        if (lineString.Crosses(polygon) || polygon.Contains(lineString))
                        {
                            intersectsIgnoredPolygon = true;
                            break;
                        }
                    }
                    
                    if (intersectsIgnoredPolygon)
                    {
                        continue;
                    }
                    
                    // Создаём ребро между текущей точкой и соседом
                    var edge = new Edge { Source = point, Target = neighborPoint };
                    edgeSet.Add(edge);
                }
            }
            
            // Добавляем саму точку в список соседей
            
            
            // Находим рёбра между соседними точками        
            FindEdgesBetweenNeighbors(ignore, sr, point, neighborSet, edgeSet);
            
            neighborSet.Add(point);
            
            result.NeighborPoints = neighborSet.ToList();
            result.Edges = edgeSet.ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing point {point}: {ex.Message}");
        }
        
        return result;
    }
    
    private void FindEdgesBetweenNeighbors(List<NetTopologySuite.Geometries.Polygon> ignore, float sr, Vector2 centerPoint, HashSet<Vector2> neighbors, HashSet<Edge> edges)
    {
        // Для каждой пары соседних точек проверяем, есть ли между ними ребро
        var neighborList = neighbors.Where(p => !p.Equals(centerPoint)).ToList();
        
        for (int i = 0; i < neighborList.Count; i++)
        {
            for (int j = i + 1; j < neighborList.Count; j++)
            {
                var pointA = neighborList[i];
                var pointB = neighborList[j];
                
                if (sr * 2 < Vector2.Distance(pointA, pointB))
                {
                    continue;
                }

                // Создаем геометрическое представление ребра
                var lineString = new LineString([
                    new Coordinate(pointA.x, pointA.y),
                    new Coordinate(pointB.x, pointB.y)
                ]);

                // Проверяем, пересекает ли ребро любой из игнорируемых полигонов
                var intersectsIgnoredPolygon = false;
                foreach (var polygon in ignore)
                {
                    if (lineString.Crosses(polygon) || polygon.Contains(lineString))
                    {
                        intersectsIgnoredPolygon = true;
                        break;
                    }
                }
                    
                if (intersectsIgnoredPolygon)
                {
                    continue;
                }
                
                // Проверяем, являются ли эти точки соседями в исходном графе
                if (ArePointsNeighbors(pointA, pointB))
                {
                    edges.Add(new Edge { Source = pointA, Target = pointB });
                }
            }
        }
    }
    
    private bool ArePointsNeighbors(Vector2 pointA, Vector2 pointB)
    {
        int indexA = _voronator.Find(pointA);
        int indexB = _voronator.Find(pointB);
        
        if (indexA < 0 || indexB < 0) return false;
        
        var neighborsOfA = _voronator.Neighbors(indexA);
        return neighborsOfA.Contains(indexB);
    }
}