using NetTopologySuite.Geometries;
using QuickGraph;
using VoronatorSharp;

namespace GraphGeneration.A;

public class VoronoiGraphAdapter
{
    public static AdjacencyGraph<Vector2, Edge<Vector2>> ConvertToQuickGraph(List<NetTopologySuite.Geometries.Polygon> ignore, Voronator voronoi, float hexSize)
    {
        var graph = new AdjacencyGraph<Vector2, Edge<Vector2>>();
        
        // Добавляем все точки Voronoi как вершины
        foreach (var site in voronoi.Delaunator.Points)
        {
            graph.AddVertex(site);
        }

        // Добавляем рёбра между соседними ячейками
        foreach (var edge in voronoi.Delaunator.GetEdges())
        {
            var sr = HexagonalGridGenerator.CalculateExpectedHexDistance(hexSize);
            
            if (sr * 2 < Vector2.Distance(edge.Item1, edge.Item2))
            {
                continue;
            }
            
            // Создаем геометрическое представление ребра
            var lineString = new LineString([
                new Coordinate(edge.Item1.x, edge.Item1.y),
                new Coordinate(edge.Item2.x, edge.Item2.y)
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

            // Если ребро пересекает игнорируемый полигон, пропускаем его
            if (intersectsIgnoredPolygon)
            {
                continue;
            }
            
            graph.AddEdge(new Edge<Vector2>(edge.Item1, edge.Item2));
        }
        
        return graph;
    }
}