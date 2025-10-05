using System.Text;
using NetTopologySuite.Geometries;
using QuickGraph;
using VoronatorSharp;
using Triangle = VoronatorSharp.Triangle;

namespace GraphGeneration;

public static class GenerateSvg
{
    public static string Generate(
        Dictionary<NetTopologySuite.Geometries.Polygon, List<Point>> pointsByPolygon,
        IReadOnlyCollection<Vector2> points,
        IReadOnlyCollection<IEdge<Vector2>> edges,
        double scale)
    {
        // Вычисляем общие границы всех полигонов
        var overallEnvelope = new Envelope();
        foreach (var polygon in pointsByPolygon.Keys)
        {
            overallEnvelope.ExpandToInclude(polygon.EnvelopeInternal);
        }

        var width = overallEnvelope.Width;
        var height = overallEnvelope.Height;

        var padding = 20;
        var svgWidth = (int)(width * scale) + padding * 2;
        var svgHeight = (int)(height * scale) + padding * 2;

        var svg = new StringBuilder();
        svg.AppendLine(
            $@"<svg width=""{svgWidth}"" height=""{svgHeight}"" viewBox=""0 0 {svgWidth} {svgHeight}"" xmlns=""http://www.w3.org/2000/svg"">");

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
        (int, int) Transform(double x, double y)
        {
            var svgX = padding + (int)((x - overallEnvelope.MinX) * scale);
            var svgY = svgHeight - padding - (int)((y - overallEnvelope.MinY) * scale);
            return (svgX, svgY);
        }

        // Рисуем граф Делоне
        svg.AppendLine("<g class=\"graph-edges\">");
        var allPoints = pointsByPolygon.Values.SelectMany(x => x).ToList();

        foreach (var triangle in edges)
        {
                var t1 = triangle.Source;
                var t2 = triangle.Target;

                // Определяем, является ли ребро межполигональным
                // var polygon1 = GetPointPolygon(new Point(t1.x, t1.y), pointsByPolygon);
                // var polygon2 = GetPointPolygon(new Point(t2.x, t2.y), pointsByPolygon);
                // var isCrossPolygon = polygon1 == polygon2 && (polygon2 != null || polygon1 != null);

                var (x1, y1) = Transform(t1.X, t1.y);
                var (x2, y2) = Transform(t2.x, t2.y);

                // if (polygon1 == polygon2)
                    svg.AppendLine(
                        $@"<line x1=""{x1}"" y1=""{y1}"" x2=""{x2}"" y2=""{y2}"" class=""{"graph-edges"}""/>");

                // string edgeClass = isCrossPolygon ? "cross-polygon-edges" : "graph-edges";
                // svg.AppendLine($@"<line x1=""{x1}"" y1=""{y1}"" x2=""{x2}"" y2=""{y2}"" class=""{"graph-edges"}""/>");
        }

        svg.AppendLine("</g>");

        var i = 0;
        // Рисуем полигоны
        foreach (var polygon in pointsByPolygon.Keys)
        {
            var polygonClass = $"polygon-{(i++ % 5) + 1}";

            svg.Append($@"<polygon class=""{polygonClass}"" points=""");
            foreach (var coord in polygon.Coordinates)
            {
                var (x, y) = Transform(coord.X, coord.Y);
                svg.Append($"{x},{y} ");
            }

            svg.AppendLine(@"""/>");
        }

        // Рисуем узлы графа
        svg.AppendLine("<g class=\"graph-nodes\">");
        foreach (var point in points)
        {
            var (x, y) = Transform(point.X, point.Y);

            // Определяем цвет и размер в зависимости от веса
            var fillColor = "#d32f2f"; // красный по умолчанию
            double radius = 10; // размер по умолчанию

            if (!point.IsPoi)
            {
                fillColor = "#008000";
            }
            else
            {
                radius = 20; // увеличенный размер для узлов с весом
            }

            svg.AppendLine($@"<circle cx=""{x}"" cy=""{y}"" r=""{radius}"" fill=""{fillColor}""/>");
        }

        svg.AppendLine("</g>");

        // Информация
        var totalPoints = allPoints.Count;
        var totalEdges = edges.Count;
        // var crossPolygonEdges = CountCrossPolygonEdges(triangles, pointsByPolygon);

        svg.AppendLine($@"<text x=""{padding}"" y=""{padding - 5}"" class=""info"">");
        svg.AppendLine($"Полигоны: {pointsByPolygon.Keys.Count}, Точки: {totalPoints}, Ребра: {totalEdges}");
        svg.AppendLine($@"</text>");

        svg.AppendLine($@"<text x=""{padding}"" y=""{padding + 15}"" class=""info"">");
        // svg.AppendLine($"Межполигональные связи: {crossPolygonEdges}");
        svg.AppendLine($@"</text>");

        svg.AppendLine("</svg>");
        return svg.ToString();
    }

    // Вспомогательные функции
    static NetTopologySuite.Geometries.Polygon? GetPointPolygon(Point point,
        Dictionary<NetTopologySuite.Geometries.Polygon, List<Point>> pointsByPolygon)
    {
        foreach (var kvp in pointsByPolygon)
        {
            if (kvp.Value.Contains(point))
                return kvp.Key;
        }

        return null;
    }

    private static int CountCrossPolygonEdges(
        IEnumerable<Triangle> triangles,
        Dictionary<NetTopologySuite.Geometries.Polygon, List<Point>> pointsByPolygon)
    {
        var count = 0;

        foreach (var triangle in triangles)
        {
            for (var i = 0; i < 3; i++)
            {
                var tPoints = triangle.ToList();
                var t1 = tPoints[i];
                var t2 = tPoints[(i + 1) % 3];

                // Определяем, является ли ребро межполигональным
                var polygon1 = GetPointPolygon(new Point(t1.x, t1.y), pointsByPolygon);
                var polygon2 = GetPointPolygon(new Point(t2.x, t2.y), pointsByPolygon);

                if (polygon1 != polygon2)
                    count++;
            }
        }

        return count / 2; // Каждое ребро считается дважды
    }
}