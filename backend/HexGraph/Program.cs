
        // Создаем тестовые полигоны

        using System.Text;
        using DeyloneMulty;
        using HexGraph;
        using NetTopologySuite.Geometries;
        using VoronatorSharp;
        using Polygon = HexGraph.Polygon;
        using Vector2 = System.Numerics.Vector2;

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
        
        // Настройки гексагонального заполнения
        var settings = new HexagonalMultiPolygonGenerator.HexagonalSettings
        {
            HexSize = 1,
            Density = 1,
            UseConvexHull = false,
            AddPolygonVertices = false,
            AddEdgePoints = false,
            EdgePointSpacing = 2f
        };
        
        // Генерируем точки
        var hexPoints = HexagonalMultiPolygonGenerator.GenerateHexagonalPoints(polygons, settings);
        
        // Создаем общую диаграмму Вороного/Делоне для всех точек
        var vectorPoints = hexPoints.Select(p => new VoronatorSharp.Vector2((float)p.X, (float)p.Y)).ToArray();
        var voronator = new Voronator(vectorPoints);

        // var graphNodes = DelaunayGraph.BuildGraphFromDelaunay(voronator.Delaunator);

        var pointsByPolygon = new Dictionary<NetTopologySuite.Geometries.Polygon, List<Point>>();

        foreach (var polygon in polygons)
        {
            var polygonPoints = new NetTopologySuite.Geometries.Polygon(new LinearRing(
                polygon.Vertices.Select(v => new Coordinate(v.X, v.Y)).ToArray()));
            pointsByPolygon[polygonPoints] = polygon.Vertices.Select(v => new Point(v.X, v.Y)).ToList();
        }
        
        string svgContent = GenerateMultiPolygonGraph.GenerateMultiPolygonGraphSvg(pointsByPolygon.Keys.ToList(), pointsByPolygon, voronator.Delaunator, 50, settings.HexSize);
        File.WriteAllText("multi_polygon_graph.svg", svgContent, Encoding.UTF8);
        
        Console.WriteLine($"Сгенерировано {hexPoints.Count} точек гексагональной сетки");
        
        // Визуализируем
        string svg = HexagonalGridVisualizer.CreateHexagonalGridSvg(polygons, hexPoints, settings.HexSize);
        SaveSvgToFile(svg, "hexagonal_grid.svg");
        
        // Создаем триангуляцию Делоне
        // var delaunay = new Voronator(hexPoints.ToArray());
        // var graph = DelaunayGraph.BuildGraphFromDelaunay(delaunay);
        
        // Сохраняем результат триангуляции
        // string triangulationSvg = SvgGraphRenderer.RenderGraphToSvg(graph);
        // SaveSvgToFile(triangulationSvg, "hexagonal_triangulation.svg");
        
        // Тест с уплотненной сеткой
        var denseSettings = new HexagonalMultiPolygonGenerator.HexagonalSettings
        {
            HexSize = 12f,
            Density = 2, // Уплотненная сетка
            AddPolygonVertices = true
        };
        
        var densePoints = HexagonalMultiPolygonGenerator.GenerateHexagonalPoints(polygons, denseSettings);
        Console.WriteLine($"Уплотненная сетка: {densePoints.Count} точек");
        
        string denseSvg = HexagonalGridVisualizer.CreateHexagonalGridSvg(polygons, densePoints, denseSettings.HexSize);
        SaveSvgToFile(denseSvg, "dense_hexagonal_grid.svg");

    
    static void SaveSvgToFile(string svgContent, string filename)
    {
        System.IO.File.WriteAllText(filename, svgContent);
    }
