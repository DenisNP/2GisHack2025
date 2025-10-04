// See https://aka.ms/new-console-template for more information

// Конфигурация

using Geometries.App;
using Geometry;

// Конфигурация
        var config = new GraphEnrichmentConfig
        {
            GridStep = 10.0,
            UseDelaunayForAvailable = true,
            MaxPointsPerZone = 200,
            ImpatienceFactor = 0.7
        };

        var service = new GraphEnrichmentService(config);

        // Тестовые данные - более интересный пример
        var zones = new List<Zone>
        {
            // Urban зона (тротуар вокруг)
            new Zone 
            { 
                Id = 1, 
                Type = ZoneType.Urban,
                Region = new List<Point>
                {
                    new(0, 0),
                    new(100, 0),
                    new(100, 100),
                    new(80, 100),
                    new (80, 20),
                    new (0, 20),

                }
            },
            // Available зона (газон внутри)
            new Zone 
            { 
                Id = 2, 
                Type = ZoneType.Available,
                Region = new List<Point>
                {
                    new(20, 20),
                    new(80, 20),
                    new(80, 80),
                    new(20, 80)
                }
            },
            // Restricted зона (здание)
            new Zone 
            { 
                Id = 3, 
                Type = ZoneType.Restricted,
                Region = new List<Point>
                {
                    new(40, 40),
                    new(60, 40),
                    new(60, 60),
                    new(40, 60)
                }
            }
        };

        var pois = new List<Poi>
        {
            new Poi { Id = 1, Point = new Point(10, 10), Weight = 1.0 },
            new Poi { Id = 2, Point = new Point(90, 90), Weight = 0.8 },
            new Poi { Id = 3, Point = new Point(50, 10), Weight = 0.5 },
            new Poi { Id = 4, Point = new Point(90, 50), Weight = 0.5 }
        };

        // Обогащение графа
        var graphPoints = service.EnrichGraph(zones, pois);

        Console.WriteLine($"Сгенерировано точек графа: {graphPoints.Count}");
        Console.WriteLine($"POI точек: {pois.Count}");
        Console.WriteLine($"Сгенерированных точек: {graphPoints.Count - pois.Count}");

        // Консольная визуализация
        ConsoleVisualizer.VisualizeZonesAndPoints(zones, graphPoints, 60, 30);

        // HTML визуализация
        var html = HtmlVisualizer.GenerateHtmlVisualization(zones, graphPoints);
        HtmlVisualizer.SaveHtmlToFile(html);

        // Дополнительная информация
        Console.WriteLine("\nСтатистика по зонам:");
        foreach (var zone in zones)
        {
            var pointsInZone = graphPoints.Count(p => GeometryUtils.IsPointInPolygon(p, zone.Region));
            Console.WriteLine($"Зона {zone.Id} ({zone.Type}): {pointsInZone} точек");
        }

        // Тестирование выбора POI
        Console.WriteLine("\nТестирование выбора POI по весам:");
        var random = new Random(42);
        var selectionCounts = new Dictionary<int, int>();
        for (int i = 0; i < 1000; i++)
        {
            var selected = service.SelectPoiByWeight(pois, random);
            selectionCounts[selected.Id] = selectionCounts.GetValueOrDefault(selected.Id) + 1;
        }

        foreach (var (id, count) in selectionCounts)
        {
            var poi = pois.First(p => p.Id == id);
            Console.WriteLine($"POI {id} (weight {poi.Weight}): {count} выборов");
        }
        
        // Экспорт во все форматы
        Console.WriteLine("\n=== ЭКСПОРТ ДАННЫХ ===");
        FileExportService.ExportToFiles(zones, graphPoints, pois, "export");