using System.Globalization;
using System.Text;
using and.Models;
using AntAlgorithm;

public class PheromoneGraphVisualizer
{
    private readonly Edge[] _edges;
    private readonly Dictionary<(int from, int to), double> _pheromones;
    private readonly Dictionary<int, Poi> _pois;
    private readonly (double minX, double minY, double maxX, double maxY) _dataBounds;
    
    public PheromoneGraphVisualizer(Edge[] edges, Dictionary<(int from, int to), double> pheromones)
    {
        _edges = edges;
        _pheromones = pheromones;
        _pois = ExtractPoisFromEdges(edges);
        _dataBounds = CalculateDataBounds();
    }

    public string GenerateSvg(int topN, int width = 5000, int height = 5000)
    {
        var normalizedPheromones = NormalizePheromones();
        var topEdges = GetTopPheromoneEdges(normalizedPheromones, topN);
        
        var svgContent = GenerateSvgContent(topEdges, normalizedPheromones, width, height);
        
        return $@"<svg width=""{width}"" height=""{height}"" xmlns=""http://www.w3.org/2000/svg"">
{svgContent}
</svg>";
    }

    private Dictionary<(int from, int to), double> NormalizePheromones()
    {
        var normalized = new Dictionary<(int from, int to), double>();
        
        if (_pheromones.Count == 0) return normalized;

        var maxPheromone = _pheromones.Values.Max();
        var minPheromone = _pheromones.Values.Min();

        foreach (var kvp in _pheromones)
        {
            var normalizedValue = maxPheromone > minPheromone 
                ? 0.1 + 0.9 * (kvp.Value - minPheromone) / (maxPheromone - minPheromone)
                : 1.0;
            
            normalized[kvp.Key] = normalizedValue;
        }

        return normalized;
    }

    private List<Edge> GetTopPheromoneEdges(Dictionary<(int from, int to), double> normalizedPheromones, int topN)
    {
        var edgePheromones = new List<(Edge edge, double pheromone)>();

        foreach (var edge in _edges)
        {
            var key1 = (edge.From.Id, edge.To.Id);
            var key2 = (edge.To.Id, edge.From.Id);
            
            double pheromone = 0;
            if (normalizedPheromones.ContainsKey(key1))
                pheromone = Math.Max(pheromone, normalizedPheromones[key1]);
            if (normalizedPheromones.ContainsKey(key2))
                pheromone = Math.Max(pheromone, normalizedPheromones[key2]);
            
            if (pheromone > 0)
            {
                edgePheromones.Add((edge, pheromone));
            }
        }

        return edgePheromones
            .OrderByDescending(x => x.pheromone)
            .Take(topN)
            .Select(x => x.edge)
            .ToList();
    }

    private string GenerateSvgContent(List<Edge> edges, Dictionary<(int from, int to), double> normalizedPheromones, int width, int height)
    {
        var svgElements = new StringBuilder();

        // Рисуем ребра
        foreach (var edge in edges)
        {
            var strokeWidth = CalculateStrokeWidth(edge, normalizedPheromones);
            var strokeColor = CalculateStrokeColor(edge, normalizedPheromones);
            
            var fromPoint = ConvertToSvgCoordinates(edge.From.Point, width, height);
            var toPoint = ConvertToSvgCoordinates(edge.To.Point, width, height);
            
            svgElements.AppendLine(
                $@"    <line x1=""{fromPoint.X.ToString(CultureInfo.InvariantCulture)}"" y1=""{fromPoint.Y.ToString(CultureInfo.InvariantCulture)}"" x2=""{toPoint.X.ToString(CultureInfo.InvariantCulture)}"" y2=""{toPoint.Y.ToString(CultureInfo.InvariantCulture)}"" 
        stroke=""{strokeColor}"" stroke-width=""{strokeWidth.ToString(CultureInfo.InvariantCulture)}"" stroke-opacity=""0.8"" />");
        }

        // Рисуем точки (POI) - только уникальные
        var drawnPoints = new HashSet<int>();
        foreach (var poi in _pois.Values.OrderBy(p => p.Id))
        {
            if (drawnPoints.Contains(poi.Id)) continue;
            
            var point = ConvertToSvgCoordinates(poi.Point, width, height);
            var fillColor = GetCircleColorByWeight(poi.Weight);
            var radius = poi.Weight > 0 ? 15 : 10;
            
            svgElements.AppendLine(
                $@"    <circle cx=""{point.X.ToString(CultureInfo.InvariantCulture)}"" cy=""{point.Y.ToString(CultureInfo.InvariantCulture)}"" r=""{radius}"" fill=""{fillColor}"" stroke=""#000"" stroke-width=""2"" />");
            
            // Подписи точек
            svgElements.AppendLine(
                 $@"    <text x=""{(point.X + 5).ToString(CultureInfo.InvariantCulture)}"" y=""{(point.Y - 10).ToString(CultureInfo.InvariantCulture)}"" font-family=""Arial"" font-size=""12"" fill=""#333"">{poi.Id}</text>");
            
            drawnPoints.Add(poi.Id);
        }

        // Легенда
        svgElements.AppendLine(GenerateLegend(width, height));

        return svgElements.ToString();
    }

    private static string GetCircleColorByWeight(double weight)
    {
        if (weight == 1.0)
        {
            return "#ff4444";
        }
        else if (weight == 0.5)
        {
            return "#ffe044";
        }
        else if (weight == 0.2)
        {
            return "#44ff54";
        }
        else
        {
            return "#4f350e";
        }
    }

    private (double X, double Y) ConvertToSvgCoordinates(Point point, int width, int height)
    {
        // Преобразуем координаты данных в координаты SVG с правильным масштабированием
        var x = MapValue(point.X, _dataBounds.minX, _dataBounds.maxX, 50, width - 50);
        
        // Для Y: инвертируем направление (математическая Y -> SVG Y)
        var y = MapValue(point.Y, _dataBounds.minY, _dataBounds.maxY, height - 50, 50);
        
        return (x, y);
    }

    private double MapValue(double value, double fromMin, double fromMax, double toMin, double toMax)
    {
        if (Math.Abs(fromMax - fromMin) < 0.0001)
            return (toMin + toMax) / 2;
            
        // Нормализуем значение от 0 до 1
        var normalized = (value - fromMin) / (fromMax - fromMin);
        
        // Преобразуем в целевую систему координат
        return toMin + normalized * (toMax - toMin);
    }

    private (double minX, double minY, double maxX, double maxY) CalculateDataBounds()
    {
        if (_pois.Count == 0)
            return (-100, -100, 100, 100);

        var minX = _pois.Values.Min(p => p.Point.X);
        var maxX = _pois.Values.Max(p => p.Point.X);
        var minY = _pois.Values.Min(p => p.Point.Y);
        var maxY = _pois.Values.Max(p => p.Point.Y);

        // Добавляем отступы (10% от диапазона, но минимум 10 единиц)
        var rangeX = maxX - minX;
        var rangeY = maxY - minY;
        
        var paddingX = Math.Max(rangeX * 0.1, 50);
        var paddingY = Math.Max(rangeY * 0.1, 50);

        // Если все точки в одном месте, создаем разумные границы
        if (rangeX < 0.001)
        {
            minX -= 100;
            maxX += 100;
        }
        if (rangeY < 0.001)
        {
            minY -= 100;
            maxY += 100;
        }

        return (minX - paddingX, minY - paddingY, maxX + paddingX, maxY + paddingY);
    }

    private double CalculateStrokeWidth(Edge edge, Dictionary<(int from, int to), double> normalizedPheromones)
    {
        var key1 = (edge.From.Id, edge.To.Id);
        var key2 = (edge.To.Id, edge.From.Id);
        
        double pheromone = 0;
        if (normalizedPheromones.ContainsKey(key1))
            pheromone = Math.Max(pheromone, normalizedPheromones[key1]);
        if (normalizedPheromones.ContainsKey(key2))
            pheromone = Math.Max(pheromone, normalizedPheromones[key2]);
        
        // Более разумный диапазон толщины линий
        return 2 + pheromone * 10;
    }

    private string CalculateStrokeColor(Edge edge, Dictionary<(int from, int to), double> normalizedPheromones)
    {
        var key1 = (edge.From.Id, edge.To.Id);
        var key2 = (edge.To.Id, edge.From.Id);
        
        double pheromone = 0;
        if (normalizedPheromones.ContainsKey(key1))
            pheromone = Math.Max(pheromone, normalizedPheromones[key1]);
        if (normalizedPheromones.ContainsKey(key2))
            pheromone = Math.Max(pheromone, normalizedPheromones[key2]);
        
        var red = (int)(pheromone * 255);
        var blue = 255 - (int)(pheromone * 200); // Сохраняем некоторую синеву
        
        return $"rgb({red}, 0, {blue})";
    }

    private Dictionary<int, Poi> ExtractPoisFromEdges(Edge[] edges)
    {
        var pois = new Dictionary<int, Poi>();
        
        foreach (var edge in edges)
        {
            if (!pois.ContainsKey(edge.From.Id))
                pois[edge.From.Id] = edge.From;
            if (!pois.ContainsKey(edge.To.Id))
                pois[edge.To.Id] = edge.To;
        }
        
        return pois;
    }

    private string GenerateLegend(int width, int height)
    {
        var legend = new StringBuilder();
        
        // Заголовок
        legend.AppendLine($@"    <text x=""50"" y=""80"" font-family=""Arial"" font-size=""28"" font-weight=""bold"" fill=""#333"">Легенда графа феромонов</text>");
        
        // Примеры линий разной толщины
        legend.AppendLine($@"    <text x=""50"" y=""120"" font-family=""Arial"" font-size=""20"" fill=""#333"">Уровень феромонов:</text>");
        
        var legendY = 160;
        for (int i = 0; i <= 4; i++)
        {
            var pheromoneLevel = i / 4.0;
            var strokeWidth = 2 + pheromoneLevel * 10;
            var color = CalculateStrokeColorByPheromone(pheromoneLevel);
            
            legend.AppendLine(
                $@"    <line x1=""50"" y1=""{legendY}"" x2=""200"" y2=""{legendY}"" 
        stroke=""{color}"" stroke-width=""{strokeWidth.ToString(CultureInfo.InvariantCulture)}"" />");
            
            legend.AppendLine(
                $@"    <text x=""220"" y=""{legendY + 8}"" font-family=""Arial"" font-size=""20"" fill=""#333"">{pheromoneLevel:P0}</text>");
            
            legendY += 50;
        }
        
        // Обозначения точек
        legendY += 30;
        legend.AppendLine($@"    <circle cx=""70"" cy=""{legendY}"" r=""10"" fill=""#4444ff"" stroke=""#000"" stroke-width=""2"" />");
        legend.AppendLine($@"    <text x=""90"" y=""{legendY + 8}"" font-family=""Arial"" font-size=""20"" fill=""#333"">Обычная точка</text>");
        
        legendY += 50;
        legend.AppendLine($@"    <circle cx=""70"" cy=""{legendY}"" r=""15"" fill=""#ff4444"" stroke=""#000"" stroke-width=""2"" />");
        legend.AppendLine($@"    <text x=""90"" y=""{legendY + 8}"" font-family=""Arial"" font-size=""20"" fill=""#333"">Точка с весом</text>");

        return legend.ToString();
    }

    private string CalculateStrokeColorByPheromone(double pheromone)
    {
        var red = (int)(pheromone * 255);
        var blue = 255 - (int)(pheromone * 200);
        return $"rgb({red}, 0, {blue})";
    }

    // Метод для отладки
    // public void DebugCoordinates()
    // {
    //     Console.WriteLine("=== ОТЛАДКА КООРДИНАТ ===");
    //     Console.WriteLine($"Границы данных: X[{_dataBounds.minX.ToString(CultureInfo.InvariantCulture)}, {_dataBounds.maxX.ToString(CultureInfo.InvariantCulture)}] Y[{_dataBounds.minY.ToString(CultureInfo.InvariantCulture)}, {_dataBounds.maxY.ToString(CultureInfo.InvariantCulture)}]");
    //     Console.WriteLine($"Всего точек: {_pois.Count}");
    //     
    //     foreach (var poi in _pois.Values.OrderBy(p => p.Id).Take(10)) // Показываем только первые 10
    //     {
    //         var svgCoords = ConvertToSvgCoordinates(poi.Point, 5000, 5000);
    //         Console.WriteLine($"Точка {poi.Id}: Data({poi.Point.X.ToString(CultureInfo.InvariantCulture)}, {poi.Point.Y.ToString(CultureInfo.InvariantCulture)}) -> SVG({svgCoords.X.ToString(CultureInfo.InvariantCulture)}, {svgCoords.Y.ToString(CultureInfo.InvariantCulture)}) Weight={poi.Weight}");
    //     }
    //     
    //     if (_pois.Count > 10)
    //         Console.WriteLine($"... и еще {_pois.Count - 10} точек");
    // }

    public void SaveToFile(string filePath, int width = 5000, int height = 5000, int topN = 500)
    {
        var myTopN = _edges.Length / 5;
        
        var svgContent = GenerateSvg(myTopN, width, height);
        File.WriteAllText(filePath, svgContent, Encoding.UTF8);
        // Console.WriteLine($"SVG сохранен в: {filePath}");
        // Console.WriteLine($"Размер канваса: {width}x{height}");
        // Console.WriteLine($"Всего точек: {_pois.Count}");
        // Console.WriteLine($"Всего ребер с феромонами: {_pheromones.Count}");
        // DebugCoordinates();
    }
}