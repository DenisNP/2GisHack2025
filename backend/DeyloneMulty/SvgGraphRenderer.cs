using VoronatorSharp;

namespace DeyloneMulty;

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

public class SvgGraphRenderer
{
    private const float NodeRadius = 3f;
    private const string NodeColor = "#ff6b6b";
    private const string EdgeColor = "#4ecdc4";
    private const string BackgroundColor = "#2d3436";
    private const string TextColor = "#ffffff";
    
    public static string RenderGraphToSvg(List<GraphNode> graph, float width = 800, float height = 600)
    {
        // Находим границы для нормализации координат
        var (minX, minY, maxX, maxY) = CalculateBounds(graph);
        var scaleX = (width - 40) / (maxX - minX);
        var scaleY = (height - 40) / (maxY - minY);
        var scale = Math.Min(scaleX, scaleY); // Сохраняем пропорции
        
        var svg = new StringBuilder();
        
        // Начало SVG документа
        svg.AppendLine($@"<svg width=""{width}"" height=""{height}"" xmlns=""http://www.w3.org/2000/svg"">");
        
        // Фон
        svg.AppendLine($@"<rect width=""100%"" height=""100%"" fill=""{BackgroundColor}""/>");
        
        // Рисуем рёбра
        svg.AppendLine("<!-- Edges -->");
        foreach (var node in graph)
        {
            var from = NormalizePoint(node.Position, minX, minY, scale, width, height);
            
            foreach (var neighborId in node.Neighbors)
            {
                if (neighborId > node.Id) // Рисуем каждое ребро только один раз
                {
                    var neighbor = graph[neighborId];
                    var to = NormalizePoint(neighbor.Position, minX, minY, scale, width, height);
                    
                    svg.AppendLine($@"<line x1=""{from.x}"" y1=""{from.y}"" x2=""{to.x}"" y2=""{to.y}"" 
                                          stroke=""{EdgeColor}"" stroke-width=""2"" />");
                }
            }
        }
        
        // Рисуем узлы
        svg.AppendLine("<!-- Nodes -->");
        foreach (var node in graph)
        {
            var point = NormalizePoint(node.Position, minX, minY, scale, width, height);
            
            svg.AppendLine($@"<circle cx=""{point.x}"" cy=""{point.y}"" r=""{NodeRadius}"" 
                                    fill=""{NodeColor}"" />");
            
            // Подписи узлов (опционально)
            svg.AppendLine($@"<text x=""{point.x + 8}"" y=""{point.y + 4}"" 
                                   font-size=""12"" fill=""{TextColor}"">{node.Id}</text>");
        }
        
        svg.AppendLine("</svg>");
        return svg.ToString();
    }
    
    private static (float minX, float minY, float maxX, float maxY) CalculateBounds(List<GraphNode> graph)
    {
        if (graph.Count == 0)
            return (0, 0, 100, 100);
            
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        
        foreach (var node in graph)
        {
            minX = Math.Min(minX, node.Position.x);
            minY = Math.Min(minY, node.Position.y);
            maxX = Math.Max(maxX, node.Position.x);
            maxY = Math.Max(maxY, node.Position.y);
        }
        
        // Добавляем отступ
        float padding = Math.Max((maxX - minX) * 0.1f, 10f);
        return (minX - padding, minY - padding, maxX + padding, maxY + padding);
    }
    
    private static (float x, float y) NormalizePoint(Vector2 point, float minX, float minY, 
                                                   float scale, float width, float height)
    {
        var x = (point.x - minX) * scale + 20; // Отступ 20px
        var y = (point.y - minY) * scale + 20;
        return (x, height - y); // Инвертируем Y для SVG системы координат
    }
}