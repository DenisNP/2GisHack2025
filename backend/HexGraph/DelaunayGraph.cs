namespace DeyloneMulty;

using System;
using System.Collections.Generic;
using System.Linq;
using VoronatorSharp;

public class GraphNode
{
    public int Id { get; set; }
    public Vector2 Position { get; set; }
    public List<int> Neighbors { get; set; } = new List<int>();
}

public class DelaunayGraph
{
    public static List<GraphNode> BuildGraphFromDelaunay(Delaunator delaunayTriangulation)
    {
        var graphNodes = new List<GraphNode>();
        var vertexToNodeMap = new Dictionary<Vector2, GraphNode>();
        
        // Создаем узлы графа для всех вершин триангуляции
        foreach (var vertex in delaunayTriangulation.Points)
        {
            var node = new GraphNode 
            { 
                Id = graphNodes.Count,
                Position = vertex
            };
            graphNodes.Add(node);
            vertexToNodeMap[vertex] = node;
        }
        
        // Добавляем связи на основе треугольников
        foreach (var triangle in delaunayTriangulation.GetTriangles())
        {
            var vertices = GetTriangleVertices(triangle);
            
            // Добавляем связи между всеми парами вершин треугольника
            for (int i = 0; i < vertices.Length; i++)
            {
                for (int j = i + 1; j < vertices.Length; j++)
                {
                    AddEdge(vertexToNodeMap[vertices[i]], vertexToNodeMap[vertices[j]]);
                }
            }
        }
        
        return graphNodes;
    }
    
    private static Vector2[] GetTriangleVertices(Triangle triangle)
    {
        // Получаем вершины треугольника (зависит от реализации библиотеки)
        // Это пример - уточните метод получения вершин для вашей версии библиотеки
        return
        [
            triangle.Point1,
            triangle.Point2,
            triangle.Point3
        ];
    }
    
    private static void AddEdge(GraphNode node1, GraphNode node2)
    {
        if (!node1.Neighbors.Contains(node2.Id))
            node1.Neighbors.Add(node2.Id);
            
        if (!node2.Neighbors.Contains(node1.Id))
            node2.Neighbors.Add(node1.Id);
    }
}