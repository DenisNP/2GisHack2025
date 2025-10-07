using System.Text;
using NetTopologySuite.Geometries;

namespace GraphGeneration.Svg;

public class SvgBuilder
{
    private readonly double _width;
    private readonly double _height;
    private readonly double _scale;
    private readonly int padding = 20;
    private readonly StringBuilder _svg;
    private Envelope _overallEnvelope;
    
    public SvgBuilder(IReadOnlyCollection<Polygon> polygons, double scale)
    {
        // Вычисляем общие границы всех полигонов
        var overallEnvelope = new Envelope();
        foreach (var polygon in polygons)
        {
            overallEnvelope.ExpandToInclude(polygon.EnvelopeInternal);
        }

        _overallEnvelope = overallEnvelope;

        _width = overallEnvelope.Width;;
        _height = overallEnvelope.Height;
        _scale = scale;
        _svg = GetBuilder();
    }
    
    public StringBuilder GetBuilder()
    {
        var svg = new StringBuilder();
        var svgWidth = (int)(_width * _scale) + padding * 2;
        var svgHeight = (int)(_height * _scale) + padding * 2;

        svg.AppendLine(
            $@"<svg width=""{svgWidth}"" height=""{svgHeight}"" viewBox=""0 0 {svgWidth} {svgHeight}"" xmlns=""http://www.w3.org/2000/svg"">");

        // Стили для разных полигонов
        svg.AppendLine(@"<defs>
        <style>
.polygon-none { 
    fill: #9e9e9e; 
    stroke: #757575; 
    stroke-width: 2; 
    fill-opacity: 0.4; 
}

.polygon-restricted { 
    fill: #f44336; 
    stroke: #d32f2f; 
    stroke-width: 2; 
    fill-opacity: 0.4; 
}

.polygon-urban { 
    fill: #ffeb3b; 
    stroke: #fbc02d; 
    stroke-width: 2; 
    fill-opacity: 0.4; 
}

.polygon-available { 
    fill: #4caf50; 
    stroke: #388e3c; 
    stroke-width: 2; 
    fill-opacity: 0.4; 
}
            .graph-edges { stroke: #666; stroke-width: 1.5; }
            .graph-nodes { /* fill теперь задается инлайн */ }
            .cross-polygon-edges { stroke: #ff9800; stroke-width: 2; stroke-dasharray: 4,2; }
            .info { font-family: Arial; font-size: 12px; fill: #666; }
            .poi-id { font-family: Arial; font-size: 12px; fill: #000; font-weight: bold; }
        </style>
    </defs>");

        // Фон
        svg.AppendLine($"<rect width=\"100%\" height=\"100%\" fill=\"white\"/>");
        


        return svg;
    }

    // Преобразование координат
    public (int, int) Transform(double x, double y)
    {
        var svgHeight = (int)(_height * _scale) + padding * 2;
        var svgX = padding + (int)((x - _overallEnvelope.MinX) * _scale);
        var svgY = padding + (int)((y - _overallEnvelope.MinY) * _scale);
        return (svgX, svgY);
    }
    
    public void Append(string text)
    {
        _svg.Append(text);
    }
    
    public void AppendText(string text)
    {
        _svg.AppendLine($@"<text x=""{padding}"" y=""{padding - 5}"" class=""info"">");
        _svg.AppendLine(text);
        _svg.AppendLine($@"</text>");
    }
    
    public void AppendLine(string text)
    {
        _svg.AppendLine(text);
    }

    public override string ToString()
    {
        _svg.AppendLine("</svg>");
        return _svg.ToString();
    }
}