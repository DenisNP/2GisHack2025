using System.Text;
using Geometry;

namespace Geometries.App;


public static class GeoFormatsGenerator
{
    // Исправленный GeoJSON генератор
    public static string GenerateGeoJson(List<Zone> zones, List<Point> graphPoints, List<Poi> pois = null)
    {
        var features = new List<string>();

        // Зоны как Polygon features
        foreach (var zone in zones)
        {
            // Используем InvariantCulture для точек вместо запятых
            var coordinates = string.Join(", ", zone.Region.Select(p => 
                $"[{p.X.ToString(System.Globalization.CultureInfo.InvariantCulture)}, " +
                $"{p.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}]"));
            
            var feature = $@"
            {{
                ""type"": ""Feature"",
                ""properties"": {{
                    ""id"": {zone.Id},
                    ""type"": ""{zone.Type}"",
                    ""name"": ""Zone_{zone.Id}_{zone.Type}""
                }},
                ""geometry"": {{
                    ""type"": ""Polygon"",
                    ""coordinates"": [[{coordinates}]]
                }}
            }}";
            features.Add(feature);
        }

        // Точки графа как MultiPoint feature
        if (graphPoints.Any())
        {
            var coordinates = string.Join(", ", graphPoints.Select(p => 
                $"[{p.X.ToString(System.Globalization.CultureInfo.InvariantCulture)}, " +
                $"{p.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}]"));
            
            var feature = $@"
            {{
                ""type"": ""Feature"",
                ""properties"": {{
                    ""name"": ""Graph_Points"",
                    ""count"": {graphPoints.Count}
                }},
                ""geometry"": {{
                    ""type"": ""MultiPoint"",
                    ""coordinates"": [{coordinates}]
                }}
            }}";
            features.Add(feature);
        }

        // POI точки как Point features
        if (pois?.Any() == true)
        {
            foreach (var poi in pois)
            {
                var feature = $@"
                {{
                    ""type"": ""Feature"",
                    ""properties"": {{
                        ""id"": {poi.Id},
                        ""weight"": {poi.Weight.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                        ""name"": ""POI_{poi.Id}""
                    }},
                    ""geometry"": {{
                        ""type"": ""Point"",
                        ""coordinates"": [
                            {poi.Point.X.ToString(System.Globalization.CultureInfo.InvariantCulture)}, 
                            {poi.Point.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}
                        ]
                    }}
                }}";
                features.Add(feature);
            }
        }

        return $@"
        {{
            ""type"": ""FeatureCollection"",
            ""features"": [{string.Join(",", features)}]
        }}";
    }

    // Остальные методы остаются без изменений...
    public static string GenerateWkt(List<Zone> zones, List<Point> graphPoints, List<Poi> pois = null)
    {
        var wkt = new StringBuilder();
        
        foreach (var zone in zones)
        {
            var polygonPoints = string.Join(", ", zone.Region.Select(p => 
                $"{p.X.ToString(System.Globalization.CultureInfo.InvariantCulture)} " +
                $"{p.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
            var zoneType = zone.Type.ToString().ToUpper();
            wkt.AppendLine($"POLYGON (({polygonPoints}))|{zoneType}|Zone_{zone.Id}");
        }

        if (graphPoints.Any())
        {
            var points = string.Join(", ", graphPoints.Select(p => 
                $"{p.X.ToString(System.Globalization.CultureInfo.InvariantCulture)} " +
                $"{p.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
            wkt.AppendLine($"MULTIPOINT ({points})|GRAPH|Graph_Points");
        }

        if (pois?.Any() == true)
        {
            foreach (var poi in pois)
            {
                wkt.AppendLine($"POINT ({poi.Point.X.ToString(System.Globalization.CultureInfo.InvariantCulture)} " +
                              $"{poi.Point.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)})|POI|POI_{poi.Id}_Weight_{poi.Weight.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            }
        }

        return wkt.ToString();
    }
    

    // KML формат (для Google Earth)
    public static string GenerateKml(List<Zone> zones, List<Point> graphPoints, List<Poi> pois = null)
    {
        var kml = new StringBuilder();
        kml.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
        kml.AppendLine(@"<kml xmlns=""http://www.opengis.net/kml/2.2"">");
        kml.AppendLine("<Document>");

        // Стили для разных типов зон
        kml.AppendLine(@"
        <Style id=""restricted_style"">
            <LineStyle><color>ff0000ff</color><width>3</width></LineStyle>
            <PolyStyle><color>440000ff</color></PolyStyle>
        </Style>
        <Style id=""urban_style"">
            <LineStyle><color>ff0088ff</color><width>2</width></LineStyle>
            <PolyStyle><color>440088ff</color></PolyStyle>
        </Style>
        <Style id=""available_style"">
            <LineStyle><color>ff00ff00</color><width>2</width></LineStyle>
            <PolyStyle><color>4400ff00</color></PolyStyle>
        </Style>
        <Style id=""graph_point_style"">
            <IconStyle><color>ff0000ff</color><scale>0.5</scale></IconStyle>
        </Style>
        <Style id=""poi_style"">
            <IconStyle><color>ffff00ff</color><scale>1.0</scale></IconStyle>
        </Style>");

        // Зоны
        foreach (var zone in zones)
        {
            var styleId = zone.Type.ToString().ToLower() + "_style";
            var coordinates = string.Join(" ", zone.Region.Select(p => $"{p.X},{p.Y},0"));
            
            kml.AppendLine($@"
            <Placemark>
                <name>Zone {zone.Id} ({zone.Type})</name>
                <styleUrl>#{styleId}</styleUrl>
                <Polygon>
                    <outerBoundaryIs>
                        <LinearRing>
                            <coordinates>{coordinates} {zone.Region[0].X},{zone.Region[0].Y},0</coordinates>
                        </LinearRing>
                    </outerBoundaryIs>
                </Polygon>
            </Placemark>");
        }

        // Точки графа
        if (graphPoints.Any())
        {
            kml.AppendLine(@"
            <Folder>
                <name>Graph Points</name>");
            
            foreach (var point in graphPoints)
            {
                kml.AppendLine($@"
                <Placemark>
                    <styleUrl>#graph_point_style</styleUrl>
                    <Point>
                        <coordinates>{point.X},{point.Y},0</coordinates>
                    </Point>
                </Placemark>");
            }
            
            kml.AppendLine("</Folder>");
        }

        // POI точки
        if (pois?.Any() == true)
        {
            kml.AppendLine(@"
            <Folder>
                <name>Points of Interest</name>");
            
            foreach (var poi in pois)
            {
                kml.AppendLine($@"
                <Placemark>
                    <name>POI {poi.Id} (weight: {poi.Weight})</name>
                    <styleUrl>#poi_style</styleUrl>
                    <Point>
                        <coordinates>{poi.Point.X},{poi.Point.Y},0</coordinates>
                    </Point>
                </Placemark>");
            }
            
            kml.AppendLine("</Folder>");
        }

        kml.AppendLine("</Document>");
        kml.AppendLine("</kml>");

        return kml.ToString();
    }

    // GPX формат (для GPS-приложений)
    public static string GenerateGpx(List<Zone> zones, List<Point> graphPoints, List<Poi> pois = null)
    {
        var gpx = new StringBuilder();
        gpx.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
        gpx.AppendLine(@"<gpx version=""1.1"" creator=""GraphEnrichmentSystem"" xmlns=""http://www.topografix.com/GPX/1/1"">");
        
        // Waypoints (POI)
        if (pois?.Any() == true)
        {
            foreach (var poi in pois)
            {
                gpx.AppendLine($@"
                <wpt lat=""{poi.Point.Y}"" lon=""{poi.Point.X}"">
                    <name>POI_{poi.Id}</name>
                    <desc>Weight: {poi.Weight}</desc>
                    <sym>Flag</sym>
                </wpt>");
            }
        }

        // Тracks (точки графа)
        if (graphPoints.Any())
        {
            gpx.AppendLine(@"
            <trk>
                <name>Graph Points</name>
                <trkseg>");
            
            foreach (var point in graphPoints)
            {
                gpx.AppendLine($@"<trkpt lat=""{point.Y}"" lon=""{point.X}""></trkpt>");
            }
            
            gpx.AppendLine(@"
                </trkseg>
            </trk>");
        }

        gpx.AppendLine("</gpx>");
        return gpx.ToString();
    }
}