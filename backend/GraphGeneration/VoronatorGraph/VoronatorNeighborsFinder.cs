using GraphGeneration.Filters;
using GraphGeneration.Models;
using VoronatorSharp;

namespace GraphGeneration.VoronatorGraph;

public class VoronatorNeighborsFinder
{
    private readonly VoronatorSharp.Voronator _voronator;
    
    public VoronatorNeighborsFinder(VoronatorSharp.Voronator voronator)
    {
        _voronator = voronator;
    }
    
    public class NeighborhoodResult
    {
        public Vector2 Point { get; set; }
        public List<Vector2> NeighborPoints { get; set; } = [];
        public List<VoronatorFinderEdge> Edges { get; set; } = [];
    }
    
    public Dictionary<Vector2, NeighborhoodResult> FindNeighborsWithEdges(
        PolygonMap polygonMap,
        float hexSize,
        IList<Vector2> partialPoints)
    {
        var result = new Dictionary<Vector2, NeighborhoodResult>();
        var edgeFilter = new EdgeFakeFilter(polygonMap, hexSize);
        
        foreach (var point in partialPoints)
        {
            result[point] = FindNeighborsWithEdges(edgeFilter, point);
        }
        
        return result;
    }
    
    public NeighborhoodResult FindNeighborsWithEdges(IEdgeFilter edgeCrossRestrictedFilter, Vector2 point)
    {
        var result = new NeighborhoodResult { Point = point };
        var neighborSet = new HashSet<Vector2>();
        var edgeSet = new HashSet<VoronatorFinderEdge>();
        
        var pointIndex = _voronator.Find(point);
        if (pointIndex < 0) return result;

        try
        {
            // Получаем соседние точки
            var neighborIndices = _voronator.Neighbors(pointIndex);
            
            foreach (var neighborIndex in neighborIndices)
            {
                // if (neighborIndex >= 0 && neighborIndex < _allPoints.Count)
                {
                    var neighborPoint = _voronator.Delaunator.Points[neighborIndex];

                    neighborSet.Add(neighborPoint);
                    
                    if (edgeCrossRestrictedFilter.Skip(point, neighborPoint))
                    {
                        continue;
                    }
                   
                    // Создаём ребро между текущей точкой и соседом
                    var edge = new VoronatorFinderEdge { Source = point, Target = neighborPoint };
                    edgeSet.Add(edge);
                }
            }
            
            // Находим рёбра между соседними точками
            FindEdgesBetweenNeighbors(edgeCrossRestrictedFilter, point, neighborSet, edgeSet);
            
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
    
    private void FindEdgesBetweenNeighbors(IEdgeFilter edgeCrossRestrictedFilter, Vector2 centerPoint, HashSet<Vector2> neighbors, HashSet<VoronatorFinderEdge> edges)
    {
        // Для каждой пары соседних точек проверяем, есть ли между ними ребро
        var neighborList = neighbors.Where(p => !p.Equals(centerPoint)).ToList();
        
        for (var i = 0; i < neighborList.Count; i++)
        {
            for (var j = i + 1; j < neighborList.Count; j++)
            {
                var pointA = neighborList[i];
                var pointB = neighborList[j];
                
                if (edgeCrossRestrictedFilter.Skip(pointA, pointB))
                {
                    continue;
                }

                // Проверяем, являются ли эти точки соседями в исходном графе
                if (ArePointsNeighbors(pointA, pointB))
                {
                    edges.Add(new VoronatorFinderEdge { Source = pointA, Target = pointB });
                }
            }
        }
    }
    
    private bool ArePointsNeighbors(Vector2 pointA, Vector2 pointB)
    {
        var indexA = _voronator.Find(pointA);
        var indexB = _voronator.Find(pointB);
        
        if (indexA < 0 || indexB < 0) return false;
        
        var neighborsOfA = _voronator.Neighbors(indexA);
        return neighborsOfA.Contains(indexB);
    }
}