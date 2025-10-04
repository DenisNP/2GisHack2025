// using NetTopologySuite.Geometries;
// using VoronatorSharp;
//
// namespace DeyloneMulty;
//
// public class MultiPolygonPointGenerator
// {
//     public static List<Vector2> GeneratePointsForMultiplePolygons(
//         List<Polygon> sourcePolygons, 
//         float pointSpacing,
//         bool useConvexHull = true)
//     {
//         // 1. Находим общий ограничивающий полигон
//         Polygon boundingPolygon = useConvexHull 
//             ? CalculateConvexHull(GetAllVertices(sourcePolygons))
//             : CalculateBoundingPolygon(sourcePolygons);
//         
//         // 2. Генерируем точки внутри общего полигона
//         var allPoints = PointGenerator.GeneratePointsInPolygon(boundingPolygon, pointSpacing);
//         
//         // 3. Фильтруем точки, оставляя только те, что внутри исходных полигонов
//         var filteredPoints = FilterPointsBySourcePolygons(allPoints, sourcePolygons);
//         
//         return filteredPoints;
//     }
//     
//     // Получить все вершины всех полигонов
//     private static List<Vector2> GetAllVertices(List<Polygon> polygons)
//     {
//         return polygons.SelectMany(p => p.Vertices).ToList();
//     }
//     
//     // Вычисление выпуклой оболочки (алгоритм Грэхема)
//     private static Polygon CalculateConvexHull(List<Vector2> points)
//     {
//         if (points.Count < 3)
//             return new Polygon(points);
//             
//         // Находим самую нижнюю левую точку
//         Vector2 pivot = points.OrderBy(p => p.Y).ThenBy(p => p.X).First();
//         
//         // Сортируем точки по полярному углу относительно pivot
//         var sortedPoints = points
//             .Where(p => p != pivot)
//             .OrderBy(p => Math.Atan2(p.Y - pivot.Y, p.X - pivot.X))
//             .ToList();
//         
//         var hull = new Stack<Vector2>();
//         hull.Push(pivot);
//         hull.Push(sortedPoints[0]);
//         
//         for (int i = 1; i < sortedPoints.Count; i++)
//         {
//             Vector2 top = hull.Pop();
//             
//             while (hull.Count > 0 && Cross(hull.Peek(), top, sortedPoints[i]) <= 0)
//             {
//                 top = hull.Pop();
//             }
//             
//             hull.Push(top);
//             hull.Push(sortedPoints[i]);
//         }
//         
//         return new Polygon(hull.Reverse());
//     }
//     
//     private static float Cross(Vector2 o, Vector2 a, Vector2 b)
//     {
//         return (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
//     }
//     
//     // Альтернативный метод: просто ограничивающий прямоугольник
//     private static Polygon CalculateBoundingPolygon(List<Polygon> polygons)
//     {
//         var allVertices = GetAllVertices(polygons);
//         
//         float minX = allVertices.Min(v => v.X);
//         float minY = allVertices.Min(v => v.Y);
//         float maxX = allVertices.Max(v => v.X);
//         float maxY = allVertices.Max(v => v.Y);
//         
//         // Создаем прямоугольник с небольшим отступом
//         float padding = Math.Min(maxX - minX, maxY - minY) * 0.1f;
//         
//         return new Polygon(new LinearRing(new[]
//         {
//             new Coordinate(minX - padding, minY - padding),
//             new Coordinate(maxX + padding, minY - padding),
//             new Coordinate(maxX + padding, maxY + padding),
//             new Coordinate(minX - padding, maxY + padding)
//         }));
//     }
//     
//     // Фильтрация точек - оставляем только те, что внутри хотя бы одного исходного полигона
//     private static List<Vector2> FilterPointsBySourcePolygons(List<Vector2> points, List<Polygon> sourcePolygons)
//     {
//         return points.Where(point => sourcePolygons.Any(polygon => polygon.ContainsPoint(point))).ToList();
//     }
// }