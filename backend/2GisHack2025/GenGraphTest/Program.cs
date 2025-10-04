
        // Создаем тестовые полигоны

        using System.Text;
        using GraphGeneration;
        using NetTopologySuite.Geometries;
        using VoronatorSharp;
        using Point = NetTopologySuite.Geometries.Point;
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

        
        
        string svgContent = GraphGenerator.Generate2(polygons, pois);
        File.WriteAllText("multi_polygon_graph.svg", svgContent, Encoding.UTF8);
        

