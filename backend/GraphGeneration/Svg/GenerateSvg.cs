using System.Globalization;
using GraphGeneration.Models;
using VoronatorSharp;

namespace GraphGeneration.Svg;

/// <summary>
/// Генерит svg по графу как есть
/// </summary>
public static class GenerateSvg
{
    private const double defaultScale = 7;

    public static string Generate(
        PolygonMap polygonMap,
        IReadOnlyCollection<GeomPoint> points,
        IReadOnlyCollection<GeomEdge> edges,
        double scale = defaultScale)
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

            // Вычисляем толщину линии в зависимости от веса (от 0.5 до 5)
            var strokeWidth = 1;

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
            double radius = 2; // размер для POI

            if (!point.IsPoi)
            {
                fillColor = "#008000"; // зелёный для обычных точек
                // Размер зависит от влияния (от 3 до 10)
                radius = Math.Max(1, Math.Min(10, 1 + point.Influence));
            }
            else
            {
                radius = 10;
            }

            svg.AppendLine($@"<circle cx=""{x}"" cy=""{y}"" r=""{radius.ToString(CultureInfo.InvariantCulture)}"" fill=""{fillColor}""/>");
            
            // Добавляем ID для POI
            if (point.IsPoi)
            {
                svg.AppendLine($@"<text x=""{x + radius + 5}"" y=""{y + 5}"" class=""poi-id"" font-size=""12"" fill=""#000"" font-family=""Arial"">{point.Id}</text>");
            }
        }

        svg.AppendLine("</g>");

        // Информация
        var totalPoints = points.Count;
        var totalEdges = edges.Count;
        svg.AppendText($"Полигоны: {polygonMap.Zones.Count}, Точки: {totalPoints}, Ребра: {totalEdges}");

        return svg.ToString();
    }
}