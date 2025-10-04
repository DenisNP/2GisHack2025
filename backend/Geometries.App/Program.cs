// See https://aka.ms/new-console-template for more information

// Конфигурация

using Geometry;

var config = new GraphEnrichmentConfig
{
    GridStep = 10.0,
    UseDelaunayForAvailable = true,
    MaxPointsPerZone = 200,
    ImpatienceFactor = 0.7
};

var service = new GraphEnrichmentService(config);

// Тестовые данные
var zones = new List<Zone>
{
    new Zone 
    { 
        Id = 1, 
        Type = ZoneType.Urban,
        Region = new List<Point>
        {
            new(0, 0),
            new(50, 0),
            new(50, 20),
            new(0, 20)
        }
    },
    new Zone 
    { 
        Id = 2, 
        Type = ZoneType.Available,
        Region = new List<Point>
        {
            new(10, 25),
            new(40, 25),
            new(40, 45),
            new(10, 45)
        }
    }
};

var pois = new List<Poi>
{
    new Poi { Id = 1, Point = new Point(5, 5), Weight = 1.0 },
    new Poi { Id = 2, Point = new Point(45, 15), Weight = 0.8 }
};

// Обогащение графа
var graphPoints = service.EnrichGraph(zones, pois);

Console.WriteLine($"Сгенерировано точек графа: {graphPoints.Count}");
Console.WriteLine($"POI точек: {pois.Count}");
Console.WriteLine($"Сгенерированных точек: {graphPoints.Count - pois.Count}");

// Пример выбора POI по весу
var selectedPoi = service.SelectPoiByWeight(pois);
Console.WriteLine($"Выбранная POI: ID={selectedPoi.Id}, Weight={selectedPoi.Weight}");

