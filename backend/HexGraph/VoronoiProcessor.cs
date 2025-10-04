// using VoronatorSharp;
// using Vector2 = System.Numerics.Vector2;
//
// namespace HexGraph;
//
// using System;
// using System.Collections.Generic;
// using System.Linq;
//
//
// public class VoronoiProcessor
// {
//     private Voronator voronator;
//     private List<Vector2> originalPoints;
//     private float maxEdgeLength;
//
//     public VoronoiProcessor(List<Vector2> points, float maxAllowedEdgeLength)
//     {
//         this.originalPoints = points;
//         this.maxEdgeLength = maxAllowedEdgeLength * 1.5f; // Полтора раза от заданного значения
//         this.voronator = new Voronator(points);
//     }
//
//     public List<List<Vector2>> GetFilteredCells()
//     {
//         var filteredCells = new List<List<Vector2>>();
//         
//         for (int i = 0; i < originalPoints.Count; i++)
//         {
//             var cell = GetFilteredCell(i);
//             if (cell.Count >= 3) // Минимум 3 точки для полигона
//             {
//                 filteredCells.Add(cell);
//             }
//         }
//         
//         return filteredCells;
//     }
//
//     private List<Vector2> GetFilteredCell(int pointIndex)
//     {
//         // Получаем исходную ячейку Вороного
//         var originalCell = voronator.GetClippedPolygon(pointIndex);
//         
//         if (originalCell.Count < 3)
//             return new List<Vector2>();
//
//         var filteredPoints = new List<Vector2> { originalCell[0] };
//         
//         for (int i = 1; i < originalCell.Count; i++)
//         {
//             var currentPoint = originalCell[i];
//             var previousPoint = filteredPoints[filteredPoints.Count - 1];
//             
//             // Проверяем длину ребра
//             var edgeLength = Vector2.Distance(previousPoint, currentPoint);
//             
//             if (edgeLength <= maxEdgeLength)
//             {
//                 filteredPoints.Add(currentPoint);
//             }
//             else
//             {
//                 // Если ребро слишком длинное, добавляем промежуточные точки
//                 var interpolatedPoints = InterpolateEdge(previousPoint, currentPoint, maxEdgeLength);
//                 filteredPoints.AddRange(interpolatedPoints);
//             }
//         }
//         
//         // Проверяем последнее ребро (замыкающее полигон)
//         var lastEdgeLength = Vector2.Distance(filteredPoints[filteredPoints.Count - 1], filteredPoints[0]);
//         if (lastEdgeLength > maxEdgeLength)
//         {
//             var interpolatedPoints = InterpolateEdge(filteredPoints[filteredPoints.Count - 1], filteredPoints[0], maxEdgeLength);
//             filteredPoints.AddRange(interpolatedPoints);
//         }
//         
//         return filteredPoints;
//     }
//
//     private List<Vector2> InterpolateEdge(Vector2 start, Vector2 end, float maxSegmentLength)
//     {
//         var points = new List<Vector2>();
//         var totalLength = Vector2.Distance(start, end);
//         var direction = (end - start).Normalized();
//         
//         int segments = (int)Math.Ceiling(totalLength / maxSegmentLength);
//         float segmentLength = totalLength / segments;
//         
//         for (int i = 1; i < segments; i++)
//         {
//             var point = start + direction * (segmentLength * i);
//             points.Add(point);
//         }
//         
//         points.Add(end);
//         return points;
//     }
//
//     // Альтернативный подход: полная реконструкция диаграммы без длинных ребер
//     public Voronator ReconstructWithoutLongEdges()
//     {
//         var validPoints = new List<Vector2>();
//         
//         // Собираем все вершины из коротких ребер
//         foreach (var cell in voronator.Delaunator.ge())
//         {
//             var filteredVertices = FilterCellVertices(cell);
//             validPoints.AddRange(filteredVertices);
//         }
//         
//         // Убираем дубликаты (можно использовать HashSet для оптимизации)
//         validPoints = validPoints.Distinct().ToList();
//         
//         return new Voronator(validPoints);
//     }
//
//     private List<Vector2> FilterCellVertices(List<Vector2> cell)
//     {
//         var validVertices = new List<Vector2>();
//         
//         for (int i = 0; i < cell.Count; i++)
//         {
//             var current = cell[i];
//             var next = cell[(i + 1) % cell.Count];
//             
//             if (Vector2.Distance(current, next) <= maxEdgeLength)
//             {
//                 validVertices.Add(current);
//             }
//         }
//         
//         return validVertices;
//     }
// }