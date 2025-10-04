using System.Text;
using NetTopologySuite.Geometries;
using VoronatorSharp;
using Triangle = VoronatorSharp.Triangle;

namespace GraphGeneration;

public static class Generatesvg
{
    public static string GenerateMultiPolygonGraphSvg(
        List<NetTopologySuite.Geometries.Polygon> ignor, 
    List<NetTopologySuite.Geometries.Polygon> polygons, 
    Dictionary<NetTopologySuite.Geometries.Polygon, List<NetTopologySuite.Geometries.Point>> pointsByPolygon,
    Delaunator voronator, 
    double scale,
        float HexSize )
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
            .graph-nodes { /* fill теперь задается инлайн */ }
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
    var triangles = voronator.GetTriangles();
    var allPoints = pointsByPolygon.Values.SelectMany(x => x).ToList();

    var sr = HexagonalGridGenerator.CalculateExpectedHexDistance(HexSize);
    
    foreach (var triangle in triangles)
    {
        for (int i = 0; i < 3; i++)
        {
            var tPoints = triangle.ToList();
            var t1 = tPoints[i];
            var t2 = tPoints[(i + 1) % 3];

            if (sr * 1.2 < Vector2.Distance(t1, t2))
            {
                continue;
            }

// Создаем геометрическое представление ребра
            var lineString = new LineString(new Coordinate[]
            {
                new Coordinate(t1.x, t1.y),
                new Coordinate(t2.x, t2.y)
            });

            // Проверяем, пересекает ли ребро любой из игнорируемых полигонов
            bool intersectsIgnoredPolygon = false;
            foreach (var polygon in ignor)
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
            
            // Определяем, является ли ребро межполигональным
            var polygon1 = GetPointPolygon(new NetTopologySuite.Geometries.Point(t1.x, t1.y), pointsByPolygon);
            var polygon2 = GetPointPolygon(new NetTopologySuite.Geometries.Point(t2.x, t2.y), pointsByPolygon);
            bool isCrossPolygon = polygon1 == polygon2 && (polygon2 != null || polygon1 != null);
            
            var (x1, y1) = transform(t1.x, t1.y);
            var (x2, y2) = transform(t2.x, t2.y);
            
            if (polygon1 == polygon2)
                svg.AppendLine($@"<line x1=""{x1}"" y1=""{y1}"" x2=""{x2}"" y2=""{y2}"" class=""{"graph-edges"}""/>");;
            
            // string edgeClass = isCrossPolygon ? "cross-polygon-edges" : "graph-edges";
            // svg.AppendLine($@"<line x1=""{x1}"" y1=""{y1}"" x2=""{x2}"" y2=""{y2}"" class=""{"graph-edges"}""/>");
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
    foreach (var point in voronator.Points)
    {
        var (x, y) = transform(point.X, point.Y);

        var lineString = new Point(x, y);

        // Проверяем, пересекает ли ребро любой из игнорируемых полигонов
        bool intersectsIgnoredPolygon = false;
        foreach (var polygon in ignor)
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
    
        // Определяем цвет и размер в зависимости от веса
        string fillColor = "#d32f2f"; // красный по умолчанию
        double radius = 2; // размер по умолчанию
    
        {
            if (point.Weight== 0)
            {
                fillColor = "#666666"; // серый для нулевого веса
                radius = 2; // обычный размер
            }
            else
            {
                fillColor = "#d32f2f"; // красный для ненулевого веса
                radius = 10; // увеличенный размер для узлов с весом
            }
        }
    
        svg.AppendLine($@"<circle cx=""{x}"" cy=""{y}"" r=""{radius}"" fill=""{fillColor}""/>");
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
static NetTopologySuite.Geometries.Polygon GetPointPolygon(NetTopologySuite.Geometries.Point point, Dictionary<NetTopologySuite.Geometries.Polygon, List<NetTopologySuite.Geometries.Point>> pointsByPolygon)
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
    Dictionary<NetTopologySuite.Geometries.Polygon, List<NetTopologySuite.Geometries.Point>> pointsByPolygon)
{
    int count = 0;
    var allPoints = pointsByPolygon.Values.SelectMany(x => x).ToList();
    
    foreach (var triangle in triangles)
    {
        for (int i = 0; i < 3; i++)
        {
            var tPoints = triangle.ToList();
            var t1 = tPoints[i];
            // var point1 = allPoints.Find(p => Math.Abs(p.X - t1.x) < 0.1 &&  Math.Abs(p.Y - t1.y) < 0.1);
            var t2 = tPoints[(i + 1) % 3];
            // var point2 = allPoints.Find(p => Math.Abs(p.X - t2.x) < 0.1 &&  Math.Abs(p.Y - t2.y) < 0.1);
            
            // Определяем, является ли ребро межполигональным
            var polygon1 = GetPointPolygon(new NetTopologySuite.Geometries.Point(t1.x, t1.y), pointsByPolygon);
            var polygon2 = GetPointPolygon(new NetTopologySuite.Geometries.Point(t2.x, t2.y), pointsByPolygon);
            
            if (polygon1 != polygon2)
                count++;
        }
    }
    return count / 2; // Каждое ребро считается дважды
}
}