using AntAlgorithm.Abstractions;
using Microsoft.Extensions.Options;

namespace AntAlgorithm;

internal sealed class AntColonyAlgorithm : IAntColonyAlgorithm
{
    private readonly Random _random = new ();
    private double[,] _pheromones;
    private DistanceWeight[,] _distances;
    private int _cityCount;

    // Параметры алгоритма
    private readonly double _alpha;
    private readonly double _beta;
    private readonly double _gamma;
    private readonly double _evaporation;
    private readonly double _q;
    private readonly int _maxIterations;

    // Сюда не просто дистанции передавать, а ещё и "вес" для разных Poi'ев.
    // Либо можно сразу сюда 
    public AntColonyAlgorithm(IOptions<AntColonyConfiguration> configuration)
    {
        _alpha = configuration.Value.Alpha;
        _beta = configuration.Value.Beta;
        _gamma = configuration.Value.Gamma;
        _evaporation = configuration.Value.Evaporation;
        _q = configuration.Value.Q;
        _maxIterations = configuration.Value.MaxIterations;
    }

    public List<Result> GetAllWays(Edge[] edges)
    {
        Init(edges);
        return Calculate().allWays;
    }

    public Path GetBestWay(Edge[] edges)
    {
        Init(edges);
        return Calculate().path;
    }

    private void Init(Edge[] edges)
    {
        // Создадим матрицу дистанций до городов
        _distances = new DistanceWeight[edges.Length,edges.Length];
        for (var i = 0; i < edges.Length; i++)
        {
            for (var j = 0; j < edges.Length; j++)
            {
                _distances[i, j] = new DistanceWeight
                {
                    Distance = MathExtensions.CalculateDistance(edges[i].From.Point, edges[j].To.Point),
                    Weight = edges[j].To.Weight,
                    From = edges[i].From,
                    To =  edges[j].To,
                };
            }
        }
        
        _cityCount = _distances.GetLength(0);
        
        _pheromones = new double[_cityCount, _cityCount];

        for (var i = 0; i < _cityCount; i++)
        {
            for (var j = 0; j < _cityCount; j++)
            {
                _pheromones[i, j] = 0.1;
            }
        }
    }

    private (List<Result> allWays, Path path) Calculate()
    {
        int[] bestPath = null!;
        var bestLength = double.MaxValue;
        
        for (var iteration = 0; iteration < _maxIterations; iteration++)
        {
            // Муравей строит путь
            var path = ConstructPath();
            var length = CalculatePathLength(path);
            
            // Обновляем лучший путь
            if (length < bestLength)
            {
                bestLength = length;
                bestPath = path;
            }
            
            // Обновляем феромоны
            UpdatePheromones(path, length);
        }
        
        var allWays = GetResult();
        var preparedBestPath = GetBestPath(bestPath);
        
        return (allWays, preparedBestPath);
    }

    private List<Result> GetResult()
    {
        List<Result> res = new List<Result>();
        
        for (var i = 0; i < _distances.GetLength(0); i++)
        {
            for (var j = 0; j < _distances.GetLength(1); j++)
            {
                res.Add(new Result
                {
                    Weight = _pheromones[i, j],
                    From = _distances[i, j].From,
                    To = _distances[i, j].To,
                });
            }
        }

        return res;
    }

    // Построить путь одного муравья
    private int[] ConstructPath()
    {
        var path = new int[_cityCount];
        var visited = new bool[_cityCount];
        
        // Начинаем со случайного города
        var currentCityNumber = _random.Next(_cityCount);
        
        // Начинаем строить путь по идее, что path[i] - содержит город, в котором находится муравей
        path[0] = currentCityNumber;
        
        // А вот visited содержит в visited[i] - номер города, в котором был муравей
        visited[currentCityNumber] = true;
        
        // Посещаем остальные города
        for (var step = 1; step < _cityCount; step++)
        {
            var nextCity = ChooseNextCity(currentCityNumber, visited);
            // Следующий шаг пути
            path[step] = nextCity;
            // Вот тут убрать посещение одного и того же
            // все посещенные на данный момент города + следующий, который будет посещенным
            visited[nextCity] = true;
            
            // ну соответственно текущий становится следующим посещенным
            currentCityNumber = nextCity;
        }


        return path;
    }
    
    // Выбираем номер города, в который нужно перейти муравью
    private int ChooseNextCity(int currentCity, bool[] visited)
    {
        // Шанс перейти в город city (m)
        var probabilities = new double[_cityCount];
        
        // сумма всех шансов
        double sum = 0;
        
        // Вычисляем вероятности для каждого города
        for (var city = 0; city < _cityCount; city++)
        {
            // Если город, для которого высчитываем вероятность посещён - пропускаем его
            // if (!visited[city])
            // {
                // Тут высчитываем количество феромонов на текущем ребре. Как подстраивается под формулу: 
                // t - _pheromones, i - currentCity, city - m
                var pheromone = Math.Pow(_pheromones[currentCity, city], _alpha);
                
                // Здесь высчитываем близость вершины графа. Как подстраивается под формулу:
                // n - _distances, i - currentCity, city - m

                double distance;
                double weight;
                
                if (_distances[currentCity, city].Distance == 0)
                {
                    distance = 0;
                }
                else
                {
                    distance = Math.Pow(_distances[currentCity, city].Distance, _beta);
                }
                
                if (_distances[currentCity, city].Weight == 0)
                {
                    weight = 1;
                }
                else
                {
                    weight = Math.Pow(_distances[currentCity, city].Weight, _gamma);
                }

                // Добавляем вес следующей вершины
                
                
                // Шанс перейти в город city (m)
                probabilities[city] = pheromone * distance * weight;
                
                // сумма всех шансов, чтобы с помощью этого делить. 
                sum += probabilities[city];
           //}
        }
        
        // С каким шансом муравей перейдет в следующий город
        var randomValue = _random.NextDouble() * sum;
        
        // Если данное число >= randomValue, значит в этот город перейдет муравей. 
        double cumulative = 0;
        
        for (var city = 0; city < _cityCount; city++)
        {
            //if (!visited[city])
            //{
                cumulative += probabilities[city];
                if (cumulative >= randomValue)
                {
                    return city;
                }
            //}
        }
        
        // Если что-то пошло не так, возвращаем первый непосещенный город
        for (var city = 0; city < _cityCount; city++)
        {
            if (!visited[city])
            {
                return city;
            }
        }
        
        // Тут вот прям сильно что-то пошло не так
        return -1;
    }
    
    // Формула обновления феромонов
    private void UpdatePheromones(int[] path, double pathLength)
    {
        // Испаряем феромоны
        for (var i = 0; i < _cityCount; i++)
        {
            for (var j = 0; j < _cityCount; j++)
            {
                _pheromones[i, j] *= (1 - _evaporation);
            }
        }
        
        // Добавляем новый феромон по формуле:
        // _q - Q, pathLength - L от k(t)
        var deltaPheromone = _q / pathLength;
        
        for (var i = 0; i < _cityCount - 1; i++)
        {
            var cityA = path[i];
            var cityB = path[i + 1];
            _pheromones[cityA, cityB] += deltaPheromone;
            _pheromones[cityB, cityA] += deltaPheromone;
        }
        
        // Замыкаем цикл (последний город -> первый город)
        
        // var lastCity = path[_cityCount - 1];
        // var firstCity = path[0];
        // _pheromones[lastCity, firstCity] += deltaPheromone;
        // _pheromones[firstCity, lastCity] += deltaPheromone;
    }

    public double CalculatePathLength(int[] path)
    {
        double length = 0;
        
        for (var i = 0; i < _cityCount - 1; i++)
        {
            length += _distances[path[i], path[i + 1]].Distance;
        }
        
        // Замыкаем цикл
        length += _distances[path[_cityCount - 1], path[0]].Distance;
        
        return length;
    }
    
    private Path GetBestPath(int[] path)
    {
        var retValue = new Path();
        var poiList =  new List<Poi>();
        
        for (var i = 0; i < _cityCount - 1; i++)
        {
            if (i == 0)
            {
                retValue.Start = _distances[path[i], path[i + 1]].From;
            }

            else if (i == _cityCount - 2)
            {
                retValue.End = _distances[path[i], path[i + 1]].To;
            }

            else
            {
                poiList.Add(_distances[path[i], path[i + 1]].From);
                poiList.Add(_distances[path[i], path[i + 1]].To);
            }
        }
        
        retValue.Points = poiList;
        
        return retValue;
    }
}