using GraphGeneration.Filters;
using GraphGeneration.Models;
using GraphGeneration.Svg;
using QuickGraph;
using VoronatorSharp;

namespace GraphGeneration;

/// <summary>
/// При генерации Svg фильтрует ребра и точки в соответствии с Зонами.
/// </summary>
public static class GenerateFilteredSvg
{
    public static string Generate(
        PolygonMap polygonMap,
        IReadOnlyCollection<Vector2> points,
        IReadOnlyCollection<IEdge<Vector2>> edges,
        double scale,
        float hexSize)
    {
        var svg = new SvgBuilder(polygonMap.Render, scale);

        // Рисуем граф 
        svg.AppendLine("<g class=\"graph-edges\">");

        var edgeFilter = new EdgeFakeFilter(polygonMap, hexSize); 

        foreach (var triangle in edges)
        {
                var t1 = triangle.Source;
                var t2 = triangle.Target;

                if (edgeFilter.Skip(t1, t2))
                {
                    continue;
                }

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
        var pointFilter = new PointFakeFilter(polygonMap);
        foreach (var point in points)
        {
            if (!point.IsPoi && pointFilter.Skip(point))
            {
                continue;
            }
            var (x, y) = svg.Transform(point.X, point.Y);

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
        svg.AppendText($"Полигоны: {polygonMap.Zones.Count}, Точки: {totalPoints}, Ребра: {edges.Count}");

        return svg.ToString();
    }
}