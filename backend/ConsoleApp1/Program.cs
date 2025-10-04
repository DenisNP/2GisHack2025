using System.Text;
using NetTopologySuite.Geometries;
using VoronatorSharp;

// Создаем многоугольник
var coordinates = new Coordinate[]
{
    new Coordinate(0, 0),
    new Coordinate(15, 0),    // разные координаты
    new Coordinate(12, 8),
    new Coordinate(5, 12),
    new Coordinate(0, 0)
};
var polygon = new Polygon(new LinearRing(coordinates));

// Получаем границы многоугольника
var envelope = polygon.EnvelopeInternal;
double minX = envelope.MinX;
double maxX = envelope.MaxX;
double minY = envelope.MinY;
double maxY = envelope.MaxY;

// Заполняем точками
double spacing = 1.0;
var points = new List<Point>();

for (double x = minX; x <= maxX; x += spacing)
{
    for (double y = minY; y <= maxY; y += spacing)
    {
        var point = new Point(x, y);
        if (polygon.Contains(point))
        {
            points.Add(point);
        }
    }
}

// Создаем диаграмму Вороного
var vectorPoints = points.Select(p => new Vector2((float)p.X, (float)p.Y)).ToArray();
var voronator = new Voronator(vectorPoints);

// Генерируем SVG с графом
string svg = GenerateGraphSvg(polygon, points, voronator, 50);
File.WriteAllText("voronoi_graph.svg", svg, Encoding.UTF8);
Console.WriteLine("Voronoi граф создан: voronoi_graph.svg");

static string GenerateVoronoiSvg(Polygon polygon, Vector2[] points, Voronator voronator, double scale = 50)
{
    var envelope = polygon.EnvelopeInternal;
    double width = envelope.MaxX - envelope.MinX;
    double height = envelope.MaxY - envelope.MinY;
    
    int padding = 30;
    int svgWidth = (int)(width * scale) + padding * 2;
    int svgHeight = (int)(height * scale) + padding * 2;
    
    var svg = new StringBuilder();
    svg.AppendLine($@"<svg width=""{svgWidth}"" height=""{svgHeight}"" viewBox=""0 0 {svgWidth} {svgHeight}"" xmlns=""http://www.w3.org/2000/svg"">");
    
    // Стили
    svg.AppendLine(@"<defs>
        <style>
            .polygon { fill: #e3f2fd; stroke: #1976d2; stroke-width: 2; fill-opacity: 0.3; }
            .points { fill: #d32f2f; }
            .voronoi-edges { stroke: #666; stroke-width: 1; stroke-dasharray: 2,2; }
            .delaunay-edges { stroke: #4caf50; stroke-width: 1.5; }
            .centers { fill: #d32f2f; }
            .info { font-family: Arial; font-size: 12px; fill: #666; }
        </style>
    </defs>");
    
    // Фон
    svg.AppendLine($"<rect width=\"100%\" height=\"100%\" fill=\"white\"/>");
    
    // Преобразование координат
    Func<double, double, (int, int)> transform = (x, y) => 
    {
        int svgX = padding + (int)((x - envelope.MinX) * scale);
        int svgY = svgHeight - padding - (int)((y - envelope.MinY) * scale);
        return (svgX, svgY);
    };
    
    // Рисуем ребра Вороного
    svg.AppendLine("<g class=\"voronoi-edges\">");
    foreach ((var A, var B) in voronator.Delaunator.GetEdges())
    {
        var (x1, y1) = transform(A.x, A.y);
        var (x2, y2) = transform(B.x, B.y);
        svg.AppendLine($@"<line x1=""{x1}"" y1=""{y1}"" x2=""{x2}"" y2=""{y2}""/>");
    }
    svg.AppendLine("</g>");
    
    // // Рисуем триангуляцию Делоне (опционально)
    // svg.AppendLine("<g class=\"delaunay-edges\">");
    // foreach (var triangle in voronator.Delaunator.GetTriangles())
    // {
    //     for (int i = 0; i < 3; i++)
    //     {
    //         var point1 = points[triangle.];
    //         var point2 = points[triangle[(i + 1) % 3]];
    //         var (x1, y1) = transform(point1.X, point1.Y);
    //         var (x2, y2) = transform(point2.X, point2.Y);
    //         svg.AppendLine($@"<line x1=""{x1}"" y1=""{y1}"" x2=""{x2}"" y2=""{y2}""/>");
    //     }
    // }
    // svg.AppendLine("</g>");
    
    // Многоугольник
    svg.Append(@"<polygon class=""polygon"" points=""");
    foreach (var coord in polygon.Coordinates)
    {
        var (x, y) = transform(coord.X, coord.Y);
        svg.Append($"{x},{y} ");
    }
    svg.AppendLine(@"""/>");
    
    // Точки (центры)
    svg.AppendLine("<g class=\"centers\">");
    foreach (var point in points)
    {
        var (x, y) = transform(point.x, point.y);
        svg.AppendLine($@"<circle cx=""{x}"" cy=""{y}"" r=""2""/>");
    }
    svg.AppendLine("</g>");
    
    // Информация
    svg.AppendLine($@"<text x=""{padding}"" y=""{padding - 5}"" class=""info"">");
    svg.AppendLine($"Points: {points.Length}, Edges: {voronator.Delaunator.GetEdges().Count()}");
    svg.AppendLine(@"</text>");
    
    svg.AppendLine("</svg>");
    return svg.ToString();
}

// Функция генерации SVG
static string GenerateSvg(Polygon polygon, List<Point> points, double scale = 50)
{
    var envelope = polygon.EnvelopeInternal;
    double width = envelope.MaxX - envelope.MinX;
    double height = envelope.MaxY - envelope.MinY;
    
    // SVG с padding
    int padding = 20;
    int svgWidth = (int)(width * scale) + padding * 2;
    int svgHeight = (int)(height * scale) + padding * 2;
    
    var svg = new StringBuilder();
    svg.AppendLine($@"<svg width=""{svgWidth}"" height=""{svgHeight}"" xmlns=""http://www.w3.org/2000/svg"">");
    svg.AppendLine($"<rect width=\"100%\" height=\"100%\" fill=\"white\"/>");
    
    // Преобразование координат в SVG координаты
    Func<double, double, (int, int)> transform = (x, y) => 
    {
        int svgX = padding + (int)((x - envelope.MinX) * scale);
        int svgY = svgHeight - padding - (int)((y - envelope.MinY) * scale); // инвертируем Y
        return (svgX, svgY);
    };
    
    // Рисуем многоугольник
    svg.AppendLine("<g>");
    svg.Append(@"<polygon points=""");
    foreach (var coord in polygon.Coordinates)
    {
        var (x, y) = transform(coord.X, coord.Y);
        svg.Append($"{x},{y} ");
    }
    svg.AppendLine(@""" fill=""lightblue"" stroke=""blue"" stroke-width=""2"" fill-opacity=""0.3""/>");
    svg.AppendLine("</g>");
    
    // Рисуем точки
    svg.AppendLine("<g>");
    foreach (var point in points)
    {
        var (x, y) = transform(point.X, point.Y);
        svg.AppendLine($@"<circle cx=""{x}"" cy=""{y}"" r=""2"" fill=""red""/>");
    }
    svg.AppendLine("</g>");
    
    svg.AppendLine("</svg>");
    return svg.ToString();
}

static string GenerateGraphSvg(Polygon polygon, List<Point> points, Voronator voronator, double scale = 50)
{
    var envelope = polygon.EnvelopeInternal;
    double width = envelope.MaxX - envelope.MinX;
    double height = envelope.MaxY - envelope.MinY;
    
    int padding = 30;
    int svgWidth = (int)(width * scale) + padding * 2;
    int svgHeight = (int)(height * scale) + padding * 2;
    
    var svg = new StringBuilder();
    svg.AppendLine($@"<svg width=""{svgWidth}"" height=""{svgHeight}"" viewBox=""0 0 {svgWidth} {svgHeight}"" xmlns=""http://www.w3.org/2000/svg"">");
    
    // Стили для графа
    svg.AppendLine(@"<defs>
        <style>
            .polygon { fill: #f5f5f5; stroke: #ccc; stroke-width: 1; }
            .graph-edges { stroke: #2196f3; stroke-width: 2; }
            .graph-nodes { fill: #ff5722; }
            .node-labels { font-family: Arial; font-size: 8px; fill: #666; }
        </style>
    </defs>");
    
    // Фон
    svg.AppendLine($"<rect width=\"100%\" height=\"100%\" fill=\"white\"/>");
    
    // Преобразование координат
    Func<double, double, (int, int)> transform = (x, y) => 
    {
        int svgX = padding + (int)((x - envelope.MinX) * scale);
        int svgY = svgHeight - padding - (int)((y - envelope.MinY) * scale);
        return (svgX, svgY);
    };
    
    // Многоугольник
    svg.Append(@"<polygon class=""polygon"" points=""");
    foreach (var coord in polygon.Coordinates)
    {
        var (x, y) = transform(coord.X, coord.Y);
        svg.Append($"{x},{y} ");
    }
    svg.AppendLine(@"""/>");
    
    // // Рисуем граф (триангуляция Делоне)
    // svg.AppendLine("<g class=\"graph-edges\">");
    // var triangles = voronator.Delaunator.GetTriangles();
    // foreach (var triangle in triangles)
    // {
    //     for (int i = 0; i < 3; i++)
    //     {
    //         var point1 = points[triangle[i]];
    //         var point2 = point1[triangle[(i + 1) % 3]];
    //         var (x1, y1) = transform(point1.X, point1.Y);
    //         var (x2, y2) = transform(point2.X, point2.Y);
    //         svg.AppendLine($@"<line x1=""{x1}"" y1=""{y1}"" x2=""{x2}"" y2=""{y2}""/>");
    //     }
    // }
    // svg.AppendLine("</g>");
    
    // Узлы графа
    svg.AppendLine("<g class=\"graph-nodes\">");
    for (int i = 0; i < points.Count; i++)
    {
        var point = points[i];
        var (x, y) = transform(point.X, point.Y);
        svg.AppendLine($@"<circle cx=""{x}"" cy=""{y}"" r=""3""/>");
        
        // Подписи узлов (опционально)
        // svg.AppendLine($@"<text x=""{x+5}"" y=""{y}"" class=""node-labels"">{i}</text>");
    }
    svg.AppendLine("</g>");
    
    // Информация
    svg.AppendLine($@"<text x=""{padding}"" y=""{padding - 5}"" class=""info"">");
    // svg.AppendLine($"Graph: {points.Count} nodes, {triangles.Count() * 3 / 2} edges");
    svg.AppendLine(@"</text>");
    
    svg.AppendLine("</svg>");
    return svg.ToString();
}