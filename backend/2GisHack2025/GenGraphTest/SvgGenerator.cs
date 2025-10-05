using System.Text;
using and.Models;
using AntAlgorithm;
using Path = AntAlgorithm.Path;

public class SvgGenerator
{
    public static void GenerateSvg(Path path, string outputFilePath = "graph.svg")
    {
        if (path == null) throw new ArgumentNullException(nameof(path));
        
        // Собираем все точки для вычисления границ
        var allPoints = GetAllPoints(path);
        
        // Вычисляем границы с отступами
        var (viewBox, width, height) = CalculateViewBox(allPoints);
        
        var svgContent = new StringBuilder();
        
        // Начало SVG документа
        svgContent.AppendLine($@"<?xml version=""1.0"" encoding=""UTF-8""?>
<svg xmlns=""http://www.w3.org/2000/svg"" 
     viewBox=""{viewBox.X} {viewBox.Y} {viewBox.Width} {viewBox.Height}""
     width=""{width}"" height=""{height}"">");
        
        // Стили
        svgContent.AppendLine(@"<style>
    .graph-edge { stroke-linecap: round; }
    .graph-node { cursor: pointer; }
</style>");

        // Создаем список ребер (связей между точками)
        var edges = CreateEdges(path);
        
        // Рисуем рёбра
        svgContent.AppendLine(@"<!-- Edges -->");
        foreach (var edge in edges)
        {
            var strokeWidth = CalculateStrokeWidth(edge.To.Weight, edges);
            var strokeColor = CalculateStrokeColor(edge.To.Weight, edges);
            var lineLength = GetLength(edge.From.Point.X, edge.To.Point.X, 
                                     edge.From.Point.Y, edge.To.Point.Y);

            svgContent.AppendLine($@"<g>
    <line
        x1=""{edge.From.Point.X}""
        y1=""{edge.From.Point.Y}""
        x2=""{edge.To.Point.X}""
        y2=""{edge.To.Point.Y}""
        stroke=""{strokeColor}""
        stroke-width=""{strokeWidth}""
        class=""graph-edge""
    />
    <text
        x=""{GetMidpoint(edge.From.Point.X, edge.To.Point.X)}""
        y=""{GetMidpoint(edge.From.Point.Y, edge.To.Point.Y)}""
        text-anchor=""middle""
        dy="".3em""
        fill=""black""
        font-size=""12""
        font-weight=""bold""
        pointer-events=""none""
    >
        {edge.From.Weight:F2}, {lineLength:F2}
    </text>
</g>");
        }

        // Рисуем вершины (точки)
        svgContent.AppendLine(@"<!-- Nodes -->");
        foreach (var node in allPoints)
        {
            var isStart = node.Id == path.Start.Id;
            var isEnd = node.Id == path.End.Id;
            var fillColor = isStart ? "#4CAF50" : (isEnd ? "#F44336" : "#2196F3");
            
            svgContent.AppendLine($@"<g>
    <circle
        cx=""{node.Point.X}""
        cy=""{node.Point.Y}""
        r=""20""
        fill=""{fillColor}""
        stroke=""#1976D2""
        stroke-width=""2""
        class=""graph-node""
    />
    <text
        x=""{node.Point.X}""
        y=""{node.Point.Y}""
        text-anchor=""middle""
        dy="".3em""
        fill=""white""
        font-size=""12""
        font-weight=""bold""
        pointer-events=""none""
    >
        {node.Id}
    </text>
</g>");
        }

        svgContent.AppendLine("</svg>");
        
        // Сохраняем в файл
        File.WriteAllText(outputFilePath, svgContent.ToString(), Encoding.UTF8);
    }

    // Функция для расчета толщины линии на основе веса
    private static double CalculateStrokeWidth(double weight, List<Edge> edges)
    {
        var minWeight = 0.0;
        var maxWeight = edges.Max(edge => edge.To.Weight);
        var minWidth = 1.0;
        var maxWidth = 8.0;

        if (Math.Abs(maxWeight - minWeight) < 0.001)
            return minWidth;

        return minWidth + ((weight - minWeight) / (maxWeight - minWeight)) * (maxWidth - minWidth);
    }

    // Функция для расчета цвета линии на основе веса
    private static string CalculateStrokeColor(double weight, List<Edge> edges)
    {
        double val = weight != 0 ? weight : 1;
        var maxWeight = edges.Max(edge => edge.To.Weight);
        
        if (Math.Abs(maxWeight) < 0.001)
            return "rgb(255, 100, 100)";

        var intensity = (int)Math.Floor((val / maxWeight) * 255);
        intensity = Math.Max(0, Math.Min(255, intensity)); // Ограничиваем диапазон
        return $"rgb({intensity}, 100, 100)";
    }

    // Вычисление длины линии
    private static double GetLength(double x1, double x2, double y1, double y2)
    {
        return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
    }

    // Вычисление средней точки
    private static double GetMidpoint(double a, double b)
    {
        return (a + b) / 2;
    }

    // Сбор всех точек пути
    private static List<Poi> GetAllPoints(Path path)
    {
        var points = new List<Poi> { path.Start };
        if (path.Points != null)
            points.AddRange(path.Points);
        points.Add(path.End);
        return points;
    }

    // Вычисление viewBox с отступами
    private static (Rectangle ViewBox, double Width, double Height) CalculateViewBox(List<Poi> points)
    {
        var minX = points.Min(p => p.Point.X);
        var maxX = points.Max(p => p.Point.X);
        var minY = points.Min(p => p.Point.Y);
        var maxY = points.Max(p => p.Point.Y);
        
        var padding = 50; // Отступ для текста и кругов
        var width = maxX - minX + padding * 2;
        var height = maxY - minY + padding * 2;
        
        var viewBox = new Rectangle
        {
            X = minX - padding,
            Y = minY - padding,
            Width = width,
            Height = height
        };
        
        return (viewBox, width, height);
    }

    // Создание ребер из пути
    private static List<Edge> CreateEdges(Path path)
    {
        var edges = new List<Edge>();
        var points = GetAllPoints(path);
        
        // Создаем связи между последовательными точками
        for (int i = 0; i < points.Count - 1; i++)
        {
            edges.Add(new Edge(points[i], points[i + 1]));
        }
        
        return edges;
    }
}

public class Rectangle
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}