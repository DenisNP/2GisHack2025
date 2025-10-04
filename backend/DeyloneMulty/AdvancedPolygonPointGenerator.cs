// using VoronatorSharp;
//
// namespace DeyloneMulty;
//
// public class AdvancedPolygonPointGenerator
// {
//     public class GenerationSettings
//     {
//         public float PointSpacing { get; set; } = 10f;
//         public int RandomPointCount { get; set; } = 0; // 0 = использовать регулярную сетку
//         public bool UseConvexHull { get; set; } = true;
//         public bool AddPolygonVertices { get; set; } = true; // Добавлять вершины полигонов
//         public float EdgePointDensity { get; set; } = 0f; // Плотность точек на рёбрах (0 = не добавлять)
//     }
//     
//     public static List<Vector2> GeneratePoints(
//         List<Polygon> sourcePolygons, 
//         GenerationSettings settings = null)
//     {
//         settings ??= new GenerationSettings();
//         
//         var points = new List<Vector2>();
//         
//         // Добавляем вершины полигонов, если нужно
//         if (settings.AddPolygonVertices)
//         {
//             points.AddRange(GetAllVertices(sourcePolygons));
//         }
//         
//         // Добавляем точки на рёбрах, если нужно
//         if (settings.EdgePointDensity > 0)
//         {
//             points.AddRange(GenerateEdgePoints(sourcePolygons, settings.EdgePointDensity));
//         }
//         
//         // Генерируем точки внутри
//         if (settings.RandomPointCount > 0)
//         {
//             points.AddRange(GenerateRandomPoints(sourcePolygons, settings.RandomPointCount));
//         }
//         else
//         {
//             points.AddRange(GenerateGridPoints(sourcePolygons, settings.PointSpacing));
//         }
//         
//         return points.Distinct().ToList(); // Убираем дубликаты
//     }
//     
//     private static List<Vector2> GenerateGridPoints(List<Polygon> sourcePolygons, float pointSpacing)
//     {
//         return MultiPolygonPointGenerator.GeneratePointsForMultiplePolygons(
//             sourcePolygons, pointSpacing, true);
//     }
//     
//     private static List<Vector2> GenerateRandomPoints(List<Polygon> sourcePolygons, int pointCount)
//     {
//         var random = new Random();
//         var points = new List<Vector2>();
//         
//         // Создаем общий bounding polygon для эффективной генерации
//         var boundingPolygon = MultiPolygonPointGenerator.CalculateConvexHull(
//             MultiPolygonPointGenerator.GetAllVertices(sourcePolygons));
//         
//         int attempts = 0;
//         int maxAttempts = pointCount * 10; // Защита от бесконечного цикла
//         
//         while (points.Count < pointCount && attempts < maxAttempts)
//         {
//             attempts++;
//             
//             // Генерируем точку в bounding polygon
//             var randomPoints = PointGenerator.GenerateRandomPointsInPolygon(
//                 boundingPolygon, 1, random);
//                 
//             if (randomPoints.Count > 0)
//             {
//                 var point = randomPoints[0];
//                 
//                 // Проверяем, что точка внутри хотя бы одного исходного полигона
//                 if (sourcePolygons.Any(p => p.ContainsPoint(point)))
//                 {
//                     points.Add(point);
//                 }
//             }
//         }
//         
//         return points;
//     }
//     
//     private static List<Vector2> GenerateEdgePoints(List<Polygon> sourcePolygons, float density)
//     {
//         var edgePoints = new List<Vector2>();
//         
//         foreach (var polygon in sourcePolygons)
//         {
//             for (int i = 0; i < polygon.Vertices.Count; i++)
//             {
//                 Vector2 start = polygon.Vertices[i];
//                 Vector2 end = polygon.Vertices[(i + 1) % polygon.Vertices.Count];
//                 
//                 float edgeLength = Vector2.Distance(start, end);
//                 int pointsOnEdge = Math.Max(1, (int)(edgeLength * density));
//                 
//                 for (int j = 1; j < pointsOnEdge; j++)
//                 {
//                     float t = (float)j / pointsOnEdge;
//                     Vector2 point = Vector2.Lerp(start, end, t);
//                     edgePoints.Add(point);
//                 }
//             }
//         }
//         
//         return edgePoints;
//     }
//     
//     private static List<Vector2> GetAllVertices(List<Polygon> polygons)
//     {
//         return polygons.SelectMany(p => p.Vertices).ToList();
//     }
// }