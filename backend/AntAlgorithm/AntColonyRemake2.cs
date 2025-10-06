using Microsoft.Extensions.Options;

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
    private int _maxAntSteps = 300;

    private int _antCount = 500;

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
            
            _pheromones[key1] = 1.0;
            _pheromones[key2] = 1.0;
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

    private Dictionary<Poi, double> GetProbabilities(Poi current, List<Poi> visited, List<Edge> availableEdges)
    {
        var probabilities = new Dictionary<Poi, double>();
        var total = 0.0;

        foreach (var edge in availableEdges)
        {
            var neighbor = edge.To.Id == current.Id ? edge.From : edge.To;
            
            // Пропускаем уже посещенные точки
            if (visited.Any(p => p.Id == neighbor.Id))
                continue;

            var pheromoneKey = (current.Id, neighbor.Id);
            var pheromone = _pheromones.ContainsKey(pheromoneKey) ? _pheromones[pheromoneKey] : 0.1;
            
            var distance = CalculateDistance(edge.From, edge.To);
            
            var weight = GetWeightValue(neighbor);
            
            var probability = Math.Pow(pheromone, _alpha) * 
                            Math.Pow(distance, _beta) *
                            Math.Pow(weight, _gamma);
            
            probabilities[neighbor] = probability;
            total += probability;
        }

        // Нормализуем вероятности
        if (total > 0)
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
        if (probabilities.Count == 0)
            return null;

        var randomValue = _random.NextDouble();
        var cumulative = 0.0;

        foreach (var (poi, probability) in probabilities)
        {
            cumulative += probability;
            if (randomValue <= cumulative)
                return poi;
        }

        // Если что-то пошло не так, возвращаем первую точку
        return probabilities.Keys.First();
    }

    public List<Ant> Run(Edge[] edges, int maxIterations = 10000)
    {
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
            EvaporatePheromones();

            // Проверка завершения
            if (ants.All(a => a.Completed || a.VisitedPois.Count >= _maxAntSteps))
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
        
        // условно 2.6
        var weightSum = notNullFromWeights.Sum(it => it.From.Weight);
        
        // условно 1000 / 2.6 = 385
        var oneAntCost = _antCount / weightSum;

        var endPointPool = GetPoolOfEndPoints(notNullFromWeights);
        
        
        foreach (var distanceWeight in notNullFromWeights)
        {
            var antCount = (int)Math.Ceiling(oneAntCost * distanceWeight.From.Weight);
            var startPoi = _edges.FirstOrDefault(it => it.From.Id == distanceWeight.From.Id).From;
            var endPointId = endPointPool[_random.Next(0,  endPointPool.Count - 1)];

            while (endPointId == startPoi.Id)
            {
                endPointId = endPointPool[_random.Next(0,  endPointPool.Count - 1)];
            }
            
            var endPoi = _edges.FirstOrDefault(it => it.From.Id == endPointId).From;

            // Создаем муравьев
            for (var i = 0; i < antCount; i++)
            {
                ants.Add(new Ant(i, startPoi, endPoi));
            }
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
        var probabilities = GetProbabilities(current, ant.VisitedPois, availableEdges);
        
        if (probabilities.Count == 0)
        {
            // Некуда идти - завершаем маршрут
            ant.Completed = true;
            return;
        }

        // Выбираем следующую точку
        var nextPoi = SelectNextPoi(current, probabilities);
        
        if (nextPoi != null)
        {
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
                    _pheromones[key1] += _q / pathQuality;
                
                if (_pheromones.ContainsKey(key2))
                    _pheromones[key2] += _q / pathQuality;
            }
        }
    }

    private double CalculatePathQuality(Ant ant)
    {
        if (ant.VisitedPois.Count < 2) return double.MaxValue;
        
        double totalDistance = 0;
        for (var i = 0; i < ant.VisitedPois.Count - 1; i++)
        {
            totalDistance += CalculateDistance(ant.VisitedPois[i], ant.VisitedPois[i + 1]);
        }

        double weight;
        // Качество пути: расстояние + штраф за длинные маршруты
        if (ant.TargetPoi?.Weight == null || ant.TargetPoi?.Weight == 0)
        {
            weight = 1;
        }
        else
        {
            weight = (double)ant.TargetPoi?.Weight;
        }

        var targetWeightBonus = weight;
        return totalDistance * targetWeightBonus;
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