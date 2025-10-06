using System.Globalization;
using GraphGeneration.Models;
using GraphGeneration.Svg;
using QuickGraph;
using VoronatorSharp;

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

        // Рисуем граф
        svg.AppendLine("<g class=\"graph-edges\">");
        foreach (var triangle in edges)
        {
            var t1 = triangle.Source;
            var t2 = triangle.Target;

            var (x1, y1) = svg.Transform(t1.X, t1.y);
            var (x2, y2) = svg.Transform(t2.x, t2.y);

            svg.AppendLine($@"<line x1=""{x1}"" y1=""{y1}"" x2=""{x2}"" y2=""{y2}"" class=""{"graph-edges"}""/>");
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

        svg.AppendText($"Полигоны: {polygonMap.Zones.Count}, Точки: {totalPoints}, Ребра: {totalEdges}");

        return svg.ToString();
    }

    public static string Generate(
        PolygonMap polygonMap,
        IReadOnlyCollection<GeomPoint> points,
        IReadOnlyCollection<GeomEdge> edges,
        double scale = 25)
    {
        var svg = new SvgBuilder(polygonMap.Render, scale);

        // Рисуем граф 
        svg.AppendLine("<g class=\"graph-edges\">");
        foreach (var triangle in edges)
        {
            var t1 = triangle.From;
            var t2 = triangle.To;

            var (x1, y1) = svg.Transform(t1.X, t1.Y);
            var (x2, y2) = svg.Transform(t2.X, t2.Y);

            if (triangle.Weight <= 3)
            {
                //continue;
            }

            // Вычисляем толщину линии в зависимости от веса (от 0.5 до 5)
            var strokeWidth = Math.Max(0.5, Math.Min(20, 0.5 + triangle.Weight * 2.5));

            svg.AppendLine($@"<line x1=""{x1}"" y1=""{y1}"" x2=""{x2}"" y2=""{y2}"" stroke=""#666"" stroke-width=""{strokeWidth.ToString(CultureInfo.InvariantCulture)}""/>");
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
            if (!point.Show)
            {
                //continue;
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
        svg.AppendText($"Полигоны: {polygonMap.Zones.Count}, Точки: {totalPoints}, Ребра: {totalEdges}");

        return svg.ToString();
    }
}