using System.Globalization;
using GraphGeneration.Models;
using GraphGeneration.Svg;
using NetTopologySuite.Geometries;
using QuickGraph;
using VoronatorSharp;
using Triangle = VoronatorSharp.Triangle;

namespace GraphGeneration;

public static class GenerateSvg
{
    public static string Generate(
        PolygonMap polygonMap,
        IReadOnlyCollection<Vector2> points,
        IReadOnlyCollection<IEdge<Vector2>> edges,
        double scale = 25)
    {
        
        var svg = new SvgBuilder(polygonMap.Render, scale);

        // Рисуем граф Делоне
        svg.AppendLine("<g class=\"graph-edges\">");
        foreach (var triangle in edges)
        {
                var t1 = triangle.Source;
                var t2 = triangle.Target;

                // Определяем, является ли ребро межполигональным
                // var polygon1 = GetPointPolygon(new Point(t1.x, t1.y), pointsByPolygon);
                // var polygon2 = GetPointPolygon(new Point(t2.x, t2.y), pointsByPolygon);
                // var isCrossPolygon = polygon1 == polygon2 && (polygon2 != null || polygon1 != null);

                var (x1, y1) = svg.Transform(t1.X, t1.y);
                var (x2, y2) = svg.Transform(t2.x, t2.y);

                // if (polygon1 == polygon2)
                    svg.AppendLine(
                        $@"<line x1=""{x1}"" y1=""{y1}"" x2=""{x2}"" y2=""{y2}"" class=""{"graph-edges"}""/>");

                // string edgeClass = isCrossPolygon ? "cross-polygon-edges" : "graph-edges";
                // svg.AppendLine($@"<line x1=""{x1}"" y1=""{y1}"" x2=""{x2}"" y2=""{y2}"" class=""{"graph-edges"}""/>");
        }

        svg.AppendLine("</g>");

        // Рисуем полигоны
        foreach (var polygon in polygonMap.Zones)
        {
            var polygonClass = $"polygon-{polygon.Type.ToString().ToLower()}";

            svg.Append($@"<polygon class=""{polygonClass}"" points=""");
            foreach (var coord in polygon.Vertices)
            {
                var (x, y) =  svg.Transform(coord.X, coord.Y);
                svg.Append($"{x},{y} ");
            }

            svg.AppendLine(@"""/>");
        }

        // Рисуем узлы графа
        svg.AppendLine("<g class=\"graph-nodes\">");
        foreach (var point in points)
        {
            var (x, y) =  svg.Transform(point.X, point.Y);

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
        var totalPoints = points.Count;
        var totalEdges = edges.Count;
        // var crossPolygonEdges = CountCrossPolygonEdges(triangles, pointsByPolygon);

        svg.AppendText($"Полигоны: {polygonMap.Zones.Count}, Точки: {totalPoints}, Ребра: {totalEdges}");

        // svg.AppendLine($@"<text x=""{padding}"" y=""{padding + 15}"" class=""info"">");
        // svg.AppendLine($"Межполигональные связи: {crossPolygonEdges}");
        // svg.AppendLine($@"</text>");

        return svg.ToString();
    }

    public static string Generate(
        PolygonMap polygonMap,
        IReadOnlyCollection<GeomPoint> points,
        IReadOnlyCollection<GeomEdge> edges,
        double scale = 25)
    {
        
        var svg = new SvgBuilder(polygonMap.Render, scale);

        // Рисуем граф Делоне
        /*svg.AppendLine("<g class=\"graph-edges\">");
        foreach (var triangle in edges)
        {
                var t1 = triangle.From;
                var t2 = triangle.To;

                // Определяем, является ли ребро межполигональным
                // var polygon1 = GetPointPolygon(new Point(t1.x, t1.y), pointsByPolygon);
                // var polygon2 = GetPointPolygon(new Point(t2.x, t2.y), pointsByPolygon);
                // var isCrossPolygon = polygon1 == polygon2 && (polygon2 != null || polygon1 != null);

                var (x1, y1) = svg.Transform(t1.X, t1.Y);
                var (x2, y2) = svg.Transform(t2.X, t2.Y);

                if (triangle.Weight <= 3)
                {
                    continue;
                }

                // Вычисляем толщину линии в зависимости от веса (от 0.5 до 5)
                var strokeWidth = Math.Max(0.5, Math.Min(20, 0.5 + triangle.Weight * 2.5));

                // if (polygon1 == polygon2)
                    svg.AppendLine($@"<line x1=""{x1}"" y1=""{y1}"" x2=""{x2}"" y2=""{y2}"" stroke=""#666"" stroke-width=""{strokeWidth.ToString(CultureInfo.InvariantCulture)}""/>");

                // string edgeClass = isCrossPolygon ? "cross-polygon-edges" : "graph-edges";
                // svg.AppendLine($@"<line x1=""{x1}"" y1=""{y1}"" x2=""{x2}"" y2=""{y2}"" class=""{"graph-edges"}""/>");
        }

        svg.AppendLine("</g>");*/

        // Рисуем полигоны
        foreach (var polygon in polygonMap.Zones)
        {
            var polygonClass = $"polygon-{polygon.Type.ToString().ToLower()}";

            svg.Append($@"<polygon class=""{polygonClass}"" points=""");
            foreach (var coord in polygon.Vertices)
            {
                var (x, y) =  svg.Transform(coord.X, coord.Y);
                svg.Append($"{x},{y} ");
            }

            svg.AppendLine(@"""/>");
        }

        // Рисуем узлы графа
        svg.AppendLine("<g class=\"graph-nodes\">");
        foreach (var point in points)
        {
            if (point.Influence <= 1)
            {
                continue;
            }
            var (x, y) =  svg.Transform(point.X, point.Y);

            // Определяем цвет и размер в зависимости от типа точки
            var fillColor = "#d32f2f"; // красный для POI
            double radius = 20; // размер для POI

            if (!point.IsPoi)
            {
                fillColor = "#008000"; // зелёный для обычных точек
                // Размер зависит от влияния (от 3 до 10)
                radius = Math.Max(3, Math.Min(10, 3 + point.Influence * 7));
            }

            svg.AppendLine($@"<circle cx=""{x}"" cy=""{y}"" r=""{radius.ToString(CultureInfo.InvariantCulture)}"" fill=""{fillColor}""/>");
        }

        svg.AppendLine("</g>");

        // Информация
        var totalPoints = points.Count;
        var totalEdges = edges.Count;
        // var crossPolygonEdges = CountCrossPolygonEdges(triangles, pointsByPolygon);

        svg.AppendText($"Полигоны: {polygonMap.Zones.Count}, Точки: {totalPoints}, Ребра: {totalEdges}");

        // svg.AppendLine($@"<text x=""{padding}"" y=""{padding + 15}"" class=""info"">");
        // svg.AppendLine($"Межполигональные связи: {crossPolygonEdges}");
        // svg.AppendLine($@"</text>");

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