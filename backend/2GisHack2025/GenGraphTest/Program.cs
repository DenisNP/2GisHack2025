
        // Создаем тестовые полигоны

        using System.Text;
        using System.Text.Json;
        using AntAlgorithm;
        using GraphGeneration;
        using VoronatorSharp;
        using Polygon = GraphGeneration.Polygon;

        var polygons = new List<Polygon>
        {
            new Polygon([
                new Vector2(0, 0),
                new Vector2(30, 0),
                new Vector2(30, 30),
                new Vector2(0, 30),
                new Vector2(0, 0)
            ]),
            new Polygon([
                new Vector2(30, 10),  // Вплотную к первому полигону
                new Vector2(50, 10),
                new Vector2(40, 30),
                new Vector2(30, 30),
                new Vector2(30, 10)
            ]),
            new Polygon([
                new Vector2(10, 30),  // Вплотную к первому полигону
                new Vector2(40, 30),
                new Vector2(40, 50),
                new Vector2(10, 50),
                new Vector2(10, 30)
            ])
        };
        
        List<Vector2> pois = [new Vector2(10001, 1, 2, 1), new Vector2(10002, 39, 18, 0.5)];

        var rr = new PolygonGenerator().GeneratePolygonsWithPois(4, 6);

        var ff = JsonSerializer.Serialize(new InputData()
        {
            Pois = rr.pois.Select(dd=> new Poi() { Id = dd.Id, Weight = dd.Weight, Point = new AntAlgorithm.Point() { X = dd.X, Y = dd.Y}}).ToArray(),
            Zones = rr.polygons.Select(ee => new Zone()
            {
                Region = ee.Vertices.Select(dd => new AntAlgorithm.Point() { X = dd.X, Y = dd.Y}).ToArray(),
                ZoneType = ee.Zone,
            }).ToArray(),
        });
        
        File.WriteAllText("multi_polygon_graph.json", ff, Encoding.UTF8);
        
        var svgContent = GraphGenerator.GenerateSvg(rr.polygons, rr.pois);
        File.WriteAllText("multi_polygon_graph.svg", svgContent, Encoding.UTF8);
        

