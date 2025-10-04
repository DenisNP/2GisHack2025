
using VoronatorApp;
using VoronatorSharp;

var polygons = new List<List<Vector2>>
{
    new List<Vector2>
    {
        new Vector2(0, 0),
        new Vector2(100, 0),
        new Vector2(100, 100),
        new Vector2(80, 100),
        new (80, 20),
        new (0, 20),    
    },  
    new List<Vector2>
    {
        new Vector2(0, 20),
        new Vector2(80, 20),
        new Vector2(80, 100),
        new Vector2(0, 100)
    }
};

        
Console.WriteLine("1. Генерация через центроиды:");
var points1 = DelaunayBasedFilling.GeneratePointsFromDelaunay(polygons, 3);


        
Console.WriteLine("\n2. Адаптивная генерация:");
var points2 = DelaunayBasedFilling.GenerateAdaptivePoints(polygons, 50f);
        
Console.WriteLine("\n3. Комбинированный подход:");
var points3 = DelaunayBasedFilling.GenerateMixedPoints(polygons, 4);
        
// Строим финальную триангуляцию
var finalDelaunay = new Delaunator(points3.ToArray());
Console.WriteLine($"\nФинальная триангуляция: {finalDelaunay.Triangles.Length / 3} треугольников");

double pointDensity = 0.01; // точек на единицу площади
var result = PolygonFiller.FillMultiplePolygonsWithDelaunay(polygons, pointDensity);

SvgExporter.ExportToSvg(polygons, result.points, null, "test.svg");


// Метод 1: Случайные точки внутри
var result1 = DelaunayBasedFilling.FillPolygonsStrictlyInside(polygons, 30);
Console.WriteLine($"Метод 1: {result1.points.Count} точек, {result1.triangles.Count} треугольников");
SvgExporter.ExportToSvg2(polygons, result1.points, result1.triangles, "FillPolygonsStrictlyInside.svg");

        
// Метод 2: Адаптивная генерация
var result2 = DelaunayBasedFilling.GenerateAdaptivePoints(polygons, 50f);
var delaunay2 = new Delaunator(result2.ToArray());
var triangles2 = DelaunayBasedFilling.GetTriangles(delaunay2)
    .Where(t => DelaunayBasedFilling.IsTriangleInsideAnyPolygon(t, polygons))
    .ToList();
        
Console.WriteLine($"Метод 2: {result2.Count} точек, {triangles2.Count} треугольников");

// Экспорт в SVG
SvgExporter.ExportToSvg2(polygons, result2, triangles2, "GenerateAdaptivePoints.svg");
        
Console.WriteLine("SVG файл создан: delaunay_inside.svg");
Console.WriteLine("Откройте файл в браузере для просмотра");