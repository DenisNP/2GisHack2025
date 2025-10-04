using NetTopologySuite.Geometries;
using System.Text;
using DeyloneMulty;
using VoronatorSharp;
using Triangle = VoronatorSharp.Triangle;

// Создаем несколько полигонов
var polygons = new List<Polygon>
{
    new Polygon(new LinearRing([
        new Coordinate(0, 0),
        new Coordinate(5, 0),
        new Coordinate(5, 5),
        new Coordinate(0, 5),
        new Coordinate(0, 0)
    ])),
    new Polygon(new LinearRing([
        new Coordinate(8, 2),
        new Coordinate(12, 2),
        new Coordinate(10, 6),
        new Coordinate(8, 6),
        new Coordinate(8, 2)
    ])),
    new Polygon(new LinearRing([
        new Coordinate(3, 7),
        new Coordinate(7, 7),
        new Coordinate(7, 10),
        new Coordinate(3, 10),
        new Coordinate(3, 7)
    ]))
};

// Заполняем все полигоны точками
double spacing = 1.0;
var allPoints = new List<Point>();
var pointsByPolygon = new Dictionary<Polygon, List<Point>>();

foreach (var polygon in polygons)
{
    var polygonPoints = FillPolygonWithPoints(polygon, spacing);
    pointsByPolygon[polygon] = polygonPoints;
    allPoints.AddRange(polygonPoints);
}

// Создаем общую диаграмму Вороного/Делоне для всех точек
var vectorPoints = allPoints.Select(p => new Vector2((float)p.X, (float)p.Y)).ToArray();
var voronator = new Voronator(vectorPoints);

var graphNodes = DelaunayGraph.BuildGraphFromDelaunay(voronator.Delaunator);

// Генерируем SVG
string svgContent = SvgGraphRenderer.RenderGraphToSvg(graphNodes);
        
// Сохраняем в файл
// SaveSvgToFile(svgContent, "delaunay_graph.svg");

// Генерируем SVG
// string svg = GenerateMultiPolygonGraphSvg(polygons, pointsByPolygon, voronator, 50);
File.WriteAllText("multi_polygon_graph.svg", svgContent, Encoding.UTF8);
Console.WriteLine($"Мультиполигональный граф создан: {allPoints.Count} точек, {polygons.Count} полигонов");

// Функция заполнения одного полигона точками
static List<Point> FillPolygonWithPoints(Polygon polygon, double spacing)
{
    var envelope = polygon.EnvelopeInternal;
    var points = new List<Point>();

    for (double x = envelope.MinX; x <= envelope.MaxX; x += spacing)
    {
        for (double y = envelope.MinY; y <= envelope.MaxY; y += spacing)
        {
            var point = new Point(x , y);
            if (polygon.Contains(point))
            {
                points.Add(point);
            }
        }
    }
    return points;
}

static string GenerateMultiPolygonGraphSvg(
    List<Polygon> polygons, 
    Dictionary<Polygon, List<Point>> pointsByPolygon,
    Voronator voronator, 
    double scale = 50)
{
    // Вычисляем общие границы всех полигонов
    var overallEnvelope = new Envelope();
    foreach (var polygon in polygons)
    {
        overallEnvelope.ExpandToInclude(polygon.EnvelopeInternal);
    }

    double width = overallEnvelope.Width;
    double height = overallEnvelope.Height;
    
    int padding = 30;
    int svgWidth = (int)(width * scale) + padding * 2;
    int svgHeight = (int)(height * scale) + padding * 2;
    
    var svg = new StringBuilder();
    svg.AppendLine($@"<svg width=""{svgWidth}"" height=""{svgHeight}"" viewBox=""0 0 {svgWidth} {svgHeight}"" xmlns=""http://www.w3.org/2000/svg"">");
    
    // Стили для разных полигонов
    svg.AppendLine(@"<defs>
        <style>
            .polygon-1 { fill: #e3f2fd; stroke: #1976d2; stroke-width: 2; fill-opacity: 0.4; }
            .polygon-2 { fill: #f3e5f5; stroke: #7b1fa2; stroke-width: 2; fill-opacity: 0.4; }
            .polygon-3 { fill: #e8f5e8; stroke: #388e3c; stroke-width: 2; fill-opacity: 0.4; }
            .polygon-4 { fill: #fff3e0; stroke: #f57c00; stroke-width: 2; fill-opacity: 0.4; }
            .polygon-5 { fill: #fbe9e7; stroke: #d84315; stroke-width: 2; fill-opacity: 0.4; }
            .graph-edges { stroke: #666; stroke-width: 1.5; }
            .graph-nodes { fill: #d32f2f; }
            .cross-polygon-edges { stroke: #ff9800; stroke-width: 2; stroke-dasharray: 4,2; }
            .info { font-family: Arial; font-size: 12px; fill: #666; }
        </style>
    </defs>");
    
    // Фон
    svg.AppendLine($"<rect width=\"100%\" height=\"100%\" fill=\"white\"/>");
    
    // Преобразование координат
    Func<double, double, (int, int)> transform = (x, y) => 
    {
        int svgX = padding + (int)((x - overallEnvelope.MinX) * scale);
        int svgY = svgHeight - padding - (int)((y - overallEnvelope.MinY) * scale);
        return (svgX, svgY);
    };
    
    // Рисуем граф Делоне
    svg.AppendLine("<g class=\"graph-edges\">");
    var triangles = voronator.Delaunator.GetTriangles();
    var allPoints = pointsByPolygon.Values.SelectMany(x => x).ToList();
    
    foreach (var triangle in triangles)
    {
        for (int i = 0; i < 3; i++)
        {
            var tPoints = triangle.ToList();
            var t1 = tPoints[i];
            var point1 = allPoints.Find(p => Math.Abs(p.X - t1.x) < 0.1 &&  Math.Abs(p.Y - t1.y) < 0.1);
            var t2 = tPoints[(i + 1) % 3];
            var point2 = allPoints.Find(p => Math.Abs(p.X - t2.x) < 0.1 &&  Math.Abs(p.Y - t2.y) < 0.1);
            var (x1, y1) = transform(point1.X, point1.Y);
            var (x2, y2) = transform(point2.X, point2.Y);
            
            // Определяем, является ли ребро межполигональным
            var polygon1 = GetPointPolygon(point1, pointsByPolygon);
            var polygon2 = GetPointPolygon(point2, pointsByPolygon);
            bool isCrossPolygon = polygon1 != polygon2;
            
            string edgeClass = isCrossPolygon ? "cross-polygon-edges" : "graph-edges";
            svg.AppendLine($@"<line x1=""{x1}"" y1=""{y1}"" x2=""{x2}"" y2=""{y2}"" class=""{edgeClass}""/>");
        }
    }
    svg.AppendLine("</g>");
    
    // Рисуем полигоны
    for (int i = 0; i < polygons.Count; i++)
    {
        var polygon = polygons[i];
        string polygonClass = $"polygon-{(i % 5) + 1}";
        
        svg.Append($@"<polygon class=""{polygonClass}"" points=""");
        foreach (var coord in polygon.Coordinates)
        {
            var (x, y) = transform(coord.X, coord.Y);
            svg.Append($"{x},{y} ");
        }
        svg.AppendLine(@"""/>");
    }
    
    // Рисуем узлы графа
    svg.AppendLine("<g class=\"graph-nodes\">");
    foreach (var point in allPoints)
    {
        var (x, y) = transform(point.X, point.Y);
        svg.AppendLine($@"<circle cx=""{x}"" cy=""{y}"" r=""2""/>");
    }
    svg.AppendLine("</g>");
    
    // Информация
    int totalPoints = allPoints.Count;
    int totalEdges = triangles.Count() * 3 / 2;
    int crossPolygonEdges = CountCrossPolygonEdges(triangles, pointsByPolygon);
    
    svg.AppendLine($@"<text x=""{padding}"" y=""{padding - 5}"" class=""info"">");
    svg.AppendLine($"Полигоны: {polygons.Count}, Точки: {totalPoints}, Ребра: {totalEdges}");
    svg.AppendLine($@"</text>");
    
    svg.AppendLine($@"<text x=""{padding}"" y=""{padding + 15}"" class=""info"">");
    svg.AppendLine($"Межполигональные связи: {crossPolygonEdges}");
    svg.AppendLine($@"</text>");
    
    svg.AppendLine("</svg>");
    return svg.ToString();
}

// Вспомогательные функции
static Polygon GetPointPolygon(Point point, Dictionary<Polygon, List<Point>> pointsByPolygon)
{
    foreach (var kvp in pointsByPolygon)
    {
        if (kvp.Value.Contains(point))
            return kvp.Key;
    }
    return null;
}

static int CountCrossPolygonEdges(
    IEnumerable<Triangle> triangles, 
    Dictionary<Polygon, List<Point>> pointsByPolygon)
{
    int count = 0;
    var allPoints = pointsByPolygon.Values.SelectMany(x => x).ToList();
    
    foreach (var triangle in triangles)
    {
        for (int i = 0; i < 3; i++)
        {
            var tPoints = triangle.ToList();
            var t1 = tPoints[i];
            var point1 = allPoints.Find(p => Math.Abs(p.X - t1.x) < 0.1 &&  Math.Abs(p.Y - t1.y) < 0.1);
            var t2 = tPoints[(i + 1) % 3];
            var point2 = allPoints.Find(p => Math.Abs(p.X - t2.x) < 0.1 &&  Math.Abs(p.Y - t2.y) < 0.1);
            
            var polygon1 = GetPointPolygon(point1, pointsByPolygon);
            var polygon2 = GetPointPolygon(point2, pointsByPolygon);
            
            if (polygon1 != polygon2)
                count++;
        }
    }
    return count / 2; // Каждое ребро считается дважды
}