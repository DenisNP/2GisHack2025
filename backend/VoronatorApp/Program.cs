
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