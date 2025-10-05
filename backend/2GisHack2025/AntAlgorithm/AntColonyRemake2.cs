using and.Models;
using AntAlgorithm.SvgGen;
using Microsoft.Extensions.Options;
using Path = and.Models.Path;

namespace AntAlgorithm;

public class Ant
{
    public int Id { get; set; }
    public Poi CurrentPosition { get; set; }
    public Poi StartPosition { get; set; }
    public List<Poi> VisitedPois { get; set; } = new List<Poi>();
    public bool Completed { get; set; }
    
    public bool HasFoundTarget { get; set; }
    public Poi? TargetPoi { get; set; }

    public Poi EndPoi { get; set; }

    public Ant(int id, Poi startPosition, Poi endPoi)
    {
        Id = id;
        CurrentPosition = startPosition;
        StartPosition = startPosition;
        EndPoi = endPoi;
        VisitedPois.Add(startPosition);
    }
}

public class AntColonyAlgorithm2
{
    private Edge[] _edges;
    private readonly Dictionary<int, Poi> _pois;
    private readonly Dictionary<(int from, int to), double> _pheromones;
    private readonly Random _random;
    
    private readonly double _alpha;
    private readonly double _beta;
    private readonly double _gamma;
    private readonly double _evaporation;
    private readonly double _q;
    private int _maxAntSteps = 400;

    private int _antCount = 10000;

    public AntColonyAlgorithm2(IOptions<AntColonyConfiguration> configuration)
    {
        _random = new Random();
        _pheromones = new Dictionary<(int, int), double>();
        _pois = new Dictionary<int, Poi>();
        
        _alpha = configuration.Value.Alpha;
        _beta = configuration.Value.Beta;
        _gamma = configuration.Value.Gamma;
        _evaporation = configuration.Value.Evaporation;
        _q = configuration.Value.Q;
    }

    private void ExtractPois()
    {
        foreach (var edge in _edges)
        {
            if (!_pois.ContainsKey(edge.From.Id))
                _pois[edge.From.Id] = edge.From;
            if (!_pois.ContainsKey(edge.To.Id))
                _pois[edge.To.Id] = edge.To;
        }
    }

    private void InitializePheromones()
    {
        foreach (var edge in _edges)
        {
            var key1 = (edge.From.Id, edge.To.Id);
            var key2 = (edge.To.Id, edge.From.Id);
            
            _pheromones[key1] = 0.1;
            _pheromones[key2] = 0.1;
        }
    }

    private double CalculateDistance(Poi from, Poi to)
    {
        var dx = from.Point.X - to.Point.X;
        var dy = from.Point.Y - to.Point.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private double GetWeightValue(Poi to)
    {
        return to.Weight > 0 ? to.Weight * 10 : 1.0;
        // var weightInfluence = to.Weight > 0 ? to.Weight : 1.0;
        // return weightInfluence / (distance + 0.1); // +0.1 чтобы избежать деления на 0
    }

    private Dictionary<Poi, double> GetProbabilities(Poi current, List<Poi> visited, List<Edge> availableEdges, Poi endPoi)
    {
        var probabilities = new Dictionary<Poi, double>();
        var total = 0.0;

        foreach (var edge in availableEdges)
        {
            var neighbor = edge.To.Id == current.Id ? edge.From : edge.To;

            // if (neighbor.Weight > 0 
            //     && availableEdges.Any(it => it.From.Weight == 0 && it.To.Weight == 0) 
            //     && visited.All(p => p.Id != neighbor.Id))
            // {
            //     continue;
            // }
            
            // Пропускаем уже посещенные точки
            if (visited.Any(p => p.Id == neighbor.Id))
                continue;

            var pheromoneKey = (current.Id, neighbor.Id);
            var pheromone = _pheromones.ContainsKey(pheromoneKey) ? _pheromones[pheromoneKey] : 0.1;

            var calculatedDistanceToEnd = CalculateDistance(neighbor, endPoi);
            
            var calculateDistanceTonNeighbor =  CalculateDistance(current, neighbor);

            // if (calculatedDistance <= HexagonalGridGenerator.CalculateExpectedHexDistance(2) * 2)
            // {
            //     calculatedDistance = HexagonalGridGenerator.CalculateExpectedHexDistance(2);
            // }

            var distanceToEnd = (1.0 / calculatedDistanceToEnd);
            var distanceToNeighbor = (1.0 / calculateDistanceTonNeighbor);
            
            //var weight = GetWeightValue(neighbor);
            
            var probability = Math.Pow(pheromone, _alpha) * 
                            Math.Pow(distanceToEnd, _beta) * 
                            Math.Pow(distanceToNeighbor, _gamma);
            
            probabilities[neighbor] = probability;
            total += probability;
        }

        if (total > 0)
        // Нормализуем вероятности
        {
            foreach (var poi in probabilities.Keys.ToList())
            {
                probabilities[poi] /= total;
            }
        }

        return probabilities;
    }

    private Poi SelectNextPoi(Poi current, Dictionary<Poi, double> probabilities)
    {
        var asd = probabilities.ToArray();
        return SelectWeightedRandom(asd, (x) => x.Value).Key;
    }
    
    private T SelectWeightedRandom<T>(T[] values, Func<T, double> func)
    {
        var listMaxSize = 1000;
    
        var nonZeroValues = values.Where(it => func(it) > 0).ToArray();

        if (nonZeroValues.Length == 0)
        {
            return values[_random.Next(values.Length)];
        }

        // Вычисляем общий вес всех элементов
        var totalWeight = nonZeroValues.Sum(v => func(v));
    
        // Вычисляем коэффициент для масштабирования до 1000 элементов
        var scaleFactor = listMaxSize / totalWeight;
    
        var list = new List<T>();

        foreach (var value in nonZeroValues)
        {
            // Вычисляем количество элементов для этого значения
            var elementsCount = (int)Math.Round(func(value) * scaleFactor);
        
            // Гарантируем, что у каждого ненулевого значения будет хотя бы 1 элемент
            elementsCount = Math.Max(1, elementsCount);
        
            for (var i = 0; i < elementsCount; i++)
            {
                list.Add(value);
            }
        }

        // Если список превысил 1000 элементов из-за округления, обрезаем его
        if (list.Count > listMaxSize)
        {
            list = list.Take(listMaxSize).ToList();
        }
    
        return list[_random.Next(list.Count)];
    }

    public List<Ant> Run(Edge[] edges, int moveCount = 1000, int maxIterations = 10000)
    {
        //var moveCount2 = 1000;
        _edges = edges;
        
        InitializePheromones();
        ExtractPois();

        var ants = InitAnts(); 

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            // Движение муравьев
            var iterator = 0;
            foreach (var ant in ants.Where(a => !a.Completed))
            {
                MoveAnt(ant);
            }

            // Обновление феромонов
            UpdatePheromones(ants);

            // Испарение феромонов
            //EvaporatePheromones();
            
            if (ants.All(a => a.Completed || a.VisitedPois.Count >= moveCount))
                break;
        }

        var preparedResult = PrepareResult(ants);

        var generator = new PheromoneGraphVisualizer(_edges, _pheromones);
        generator.SaveToFile("test.svg");
        
        // 1. Вернуть самые ОФЕРАМОННЕНЫЕ пути !@#$%
        // 2. Сделать так, чтобы муравьи шли до своей конечной точки+
        return ants;
    }

    private List<Path> PrepareResult(List<Ant> ants)
    {
        var pathList = new List<Path>();
        
        foreach (var antThatFoundTarget in ants.Where(it => it.HasFoundTarget))
        {
            var path = new Path();
            path.Start = antThatFoundTarget.VisitedPois[0];
            path.End = antThatFoundTarget.EndPoi;
            path.Points =  antThatFoundTarget.VisitedPois.Skip(1).ToList();
            pathList.Add(path);
        }
        
        return pathList;
    }

    private List<int> GetPoolOfEndPoints(List<Edge> notNullFromWeights)
    {
        var listIds =  new List<int>();
        var minWeight = notNullFromWeights.MinBy(it => it.From.Weight).From.Weight;
        var coeff = 1 / minWeight;

        foreach (var weight in notNullFromWeights)
        {
            for (var i = 0; i < weight.From.Weight * coeff; i++)
            {
                listIds.Add(weight.From.Id);
            }
        }
        
        return listIds;
    }

    private List<Ant> InitAnts()
    {
        // Было
        // var ants = new List<Ant>();
        //
        // var startPoi = _edges.MaxBy(it => it.From.Weight).From;
        //
        // // Создаем муравьев
        // for (var i = 0; i < _antCount; i++)
        // {
        //     ants.Add(new Ant(i, startPoi));
        // }
        
        // Стало
        var ants = new List<Ant>();
        var notNullFromWeights = _edges.Where(it => it.From.Weight != 0).ToList();
        
        var notNullPoiWeights = _edges.SelectMany(edge => new List<Poi> {edge.From, edge.To } ).Where(it => it.Weight > 0).ToArray();
        for (var i = 0; i < _antCount; i++)
        {
            // var start = _edges.SelectMany(it => new List<Poi> { it.From, it.To }).FirstOrDefault(it => it.Id == 41);
            // var end = _edges.SelectMany(it => new List<Poi> { it.From, it.To }).FirstOrDefault(it => it.Id == 10);
            
            var poi = SelectWeightedRandom(notNullPoiWeights, (x) => Math.Sqrt(x.Weight));
            
            var endPoint = SelectWeightedRandom(notNullPoiWeights.Except([poi]).ToArray(), (x) => Math.Sqrt(x.Weight));
            
            ants.Add(new Ant(i, poi, endPoint));
        }

        return ants;
    }

    private void MoveAnt(Ant ant)
    {
        var current = ant.CurrentPosition;
        
        // Получаем доступные ребра из текущей позиции
        var availableEdges = _edges.Where(e => 
            e.From.Id == current.Id || e.To.Id == current.Id).ToList();

        // Получаем вероятности перехода
        var probabilities = GetProbabilities(current, ant.VisitedPois, availableEdges, ant.EndPoi);
        
        if (probabilities.Count == 0)
        {
            // Некуда идти - завершаем маршрут
            ant.Completed = true;
            return;
        }

        // Выбираем следующую точку
        var nextPoi = SelectNextPoi(current, probabilities);
        
        ant.CurrentPosition = nextPoi;
        ant.VisitedPois.Add(nextPoi);
            
        // Проверяем, нашли ли мы целевую точку (с весом)
        if (nextPoi.Id == ant.EndPoi.Id)
        {
            ant.Completed = true;
            ant.HasFoundTarget = true;
            ant.TargetPoi = nextPoi;
        }
    }

    private void UpdatePheromones(List<Ant> ants)
    {
        foreach (var ant in ants.Where(a => a.Completed))
        {
            var pathQuality = CalculatePathQuality(ant);
            
            for (var i = 0; i < ant.VisitedPois.Count - 1; i++)
            {
                var from = ant.VisitedPois[i];
                var to = ant.VisitedPois[i + 1];
                
                var key1 = (from.Id, to.Id);
                var key2 = (to.Id, from.Id);
                
                if (_pheromones.ContainsKey(key1))
                    _pheromones[key1] += pathQuality;
                
                if (_pheromones.ContainsKey(key2))
                    _pheromones[key2] += pathQuality;
            }
        }
    }

    private double CalculatePathQuality(Ant ant)
    {
        double totalDistance = 0;
        for (var i = 0; i < ant.VisitedPois.Count - 1; i++)
        {
            totalDistance += CalculateDistance(ant.VisitedPois[i], ant.VisitedPois[i + 1]);
        }
        
        return 1.0 / totalDistance;
    }

    private void EvaporatePheromones()
    {
        var keys = _pheromones.Keys.ToList();
        foreach (var key in keys)
        {
            _pheromones[key] *= (1 - _evaporation);
            // Минимальный уровень феромонов
            if (_pheromones[key] < 0.1)
                _pheromones[key] = 0.1;
        }
    }
}