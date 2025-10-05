using and.Models;
using Microsoft.Extensions.Options;

namespace AntAlgorithm;

public class AntColonyAlgorithmRemake
{
    private static Random random = new Random(0);
    
    private readonly double _alpha;
    private readonly double _beta;
    private readonly double _gamma;
    private readonly double _evaporation;
    private readonly double _q;

    private class IdDistMap
    {
        public int FromId { get; set; }
        public int ToId { get; set; }
        public double Dist { get; set; }
    }

    public AntColonyAlgorithmRemake(IOptions<AntColonyConfiguration> configuration)
    {
        _alpha = configuration.Value.Alpha;
        _beta = configuration.Value.Beta;
        _gamma = configuration.Value.Gamma;
        _evaporation = configuration.Value.Evaporation;
        _q = configuration.Value.Q;
    }

    public List<Path> Calculate(Edge[] edges)
    {
        var numCities = edges.Length;
        var numAnts = 100;
        var maxTime = 1000;

        var dists = MakeGraphDistances(numCities, edges);
        var ants = InitAnts(numAnts, numCities);
        var pheromones = InitPheromones(numCities);

        var time = 0;
        while (time < maxTime)
        {
            UpdateAnts(ants, pheromones, dists);
            UpdatePheromones(pheromones, ants, dists);

            time += 1;
        }

        return new List<Path>();
    }

    private void UpdatePheromones(double[][] pheromones, int[][] ants, IdDistMap[,] dists)
    {
        for (var i = 0; i <= pheromones.Length - 1; i++)
        {
            for (var j = i + 1; j <= pheromones[i].Length - 1; j++)
            {
                for (var k = 0; k <= ants.Length - 1; k++)
                {
                    var length = Length(ants[k], dists);
                    // length of ant k trail
                    var decrease = (1.0 - _evaporation) * pheromones[i][j];
                    var increase = 0.0;
                    if (EdgeInTrail(i, j, ants[k]) == true)
                    {
                        increase = (_q / length);
                    }

                    pheromones[i][j] = decrease + increase;

                    if (pheromones[i][j] < 0.0001)
                    {
                        pheromones[i][j] = 0.0001;
                    }
                    else if (pheromones[i][j] > 100000.0)
                    {
                        pheromones[i][j] = 100000.0;
                    }

                    pheromones[j][i] = pheromones[i][j];
                }
            }
        }
    }
    
    private static bool EdgeInTrail(int cityX, int cityY, int[] trail)
    {
        // are cityX and cityY adjacent to each other in trail[]?
        var lastIndex = trail.Length - 1;
        var idx = IndexOfTarget(trail, cityX);

        if (idx == 0 && trail[1] == cityY)
        {
            return true;
        }
        else if (idx == 0 && trail[lastIndex] == cityY)
        {
            return true;
        }
        else if (idx == 0)
        {
            return false;
        }
        else if (idx == lastIndex && trail[lastIndex - 1] == cityY)
        {
            return true;
        }
        else if (idx == lastIndex && trail[0] == cityY)
        {
            return true;
        }
        else if (idx == lastIndex)
        {
            return false;
        }
        else if (trail[idx - 1] == cityY)
        {
            return true;
        }
        else if (trail[idx + 1] == cityY)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void UpdateAnts(int[][] ants, double[][] pheromones, IdDistMap[,] dists)
    {
        var numCities = pheromones.Length;
        for (var k = 0; k <= ants.Length - 1; k++)
        {
            var start = random.Next(0, numCities);
            var newTrail = BuildTrail(k, start, pheromones, dists);
            ants[k] = newTrail;
        }
    }

    private int[] BuildTrail(int k, int start, double[][] pheromones, IdDistMap[,] dists)
    {
        var numCities = pheromones.Length;
        var trail = new int[numCities];
        var visited = new bool[numCities];
        trail[0] = start;
        visited[start] = true;
        for (var i = 0; i <= numCities - 2; i++)
        {
            var cityX = trail[i];
            var next = NextCity(cityX, visited, pheromones, dists);
            trail[i + 1] = next;
            visited[next] = true;
        }
        return trail;
    }

    private int NextCity(int cityX, bool[] visited, double[][] pheromones, IdDistMap[,] dists)
    {
        // for ant k (with visited[]), at nodeX, what is next node in trail?
        var probs = MoveProbs(cityX, visited, pheromones, dists);

        var cumul = new double[probs.Length + 1];
        for (var i = 0; i <= probs.Length - 1; i++)
        {
            cumul[i + 1] = cumul[i] + probs[i];
            // consider setting cumul[cuml.Length-1] to 1.00
        }

        var p = random.NextDouble();

        for (var i = 0; i <= cumul.Length - 2; i++)
        {
            if (p >= cumul[i] && p < cumul[i + 1])
            {
                return i;
            }
        }
        throw new Exception("Failure to return valid city in NextCity");
    }

    private double[] MoveProbs(int cityX, bool[] visited, double[][] pheromones, IdDistMap[,] dists)
    {
        // for ant k, located at nodeX, with visited[], return the prob of moving to each city
        var numCities = pheromones.Length;
        var taueta = new double[numCities];
        // inclues cityX and visited cities
        var sum = 0.0;
        // sum of all tauetas
        // i is the adjacent city
        for (var i = 0; i <= taueta.Length - 1; i++)
        {
            if (i == cityX)
            {
                taueta[i] = 0.0;
                // prob of moving to self is 0
            }
            else if (visited[i])
            {
                taueta[i] = 0.0;
                // prob of moving to a visited city is 0
            }
            else
            {
                taueta[i] = Math.Pow(pheromones[cityX][i], _alpha) * Math.Pow((1.0 / Distance(cityX, i, dists).Dist), _beta);
                // could be huge when pheromone[][] is big
                if (taueta[i] < 0.0001)
                {
                    taueta[i] = 0.0001;
                }
                else if (taueta[i] > (double.MaxValue / (numCities * 100)))
                {
                    taueta[i] = double.MaxValue / (numCities * 100);
                }
            }
            sum += taueta[i];
        }

        var probs = new double[numCities];
        for (var i = 0; i <= probs.Length - 1; i++)
        {
            probs[i] = taueta[i] / sum;
            // big trouble if sum = 0.0
        }
        return probs;
    }

    private IdDistMap Distance(int cityX, int cityY, IdDistMap[,] dists)
    {
        return dists[cityX, cityY];
    }

    private double[][] InitPheromones(int numCities)
    {
        var pheromones = new double[numCities][];
        for (var i = 0; i <= numCities - 1; i++)
        {
            pheromones[i] = new double[numCities];
        }
        for (var i = 0; i <= pheromones.Length - 1; i++)
        {
            for (var j = 0; j <= pheromones[i].Length - 1; j++)
            {
                pheromones[i][j] = 0.01;
                // otherwise first call to UpdateAnts -> BuiuldTrail -> NextNode -> MoveProbs => all 0.0 => throws
            }
        }
        return pheromones;
    }

    private int[][] InitAnts(int numAnts, int numCities)
    {
        var ants = new int[numAnts][];
        for (var k = 0; k < numAnts - 1; k++)
        {
            var start = random.Next(0, numCities);
            ants[k] = RandomTrail(start, numCities);
        }

        return ants;
    }

    private int[] RandomTrail(int start, int numCities)
    {
        // helper for InitAnts
        var trail = new int[numCities];

        // sequential
        for (var i = 0; i <= numCities - 1; i++)
        {
            trail[i] = i;
        }

        // Fisher-Yates shuffle
        for (var i = 0; i <= numCities - 1; i++)
        {
            var r = random.Next(i, numCities);
            var tmp = trail[r];
            trail[r] = trail[i];
            trail[i] = tmp;
        }

        var idx = IndexOfTarget(trail, start);
        // put start at [0]
        var temp = trail[0];
        trail[0] = trail[idx];
        trail[idx] = temp;

        return trail;
    }
    
    private static int IndexOfTarget(int[] trail, int target)
    {
        // helper for RandomTrail
        for (var i = 0; i <= trail.Length - 1; i++)
        {
            if (trail[i] == target)
            {
                return i;
            }
        }
        throw new Exception("Target not found in IndexOfTarget");
    }
    
    private double Length(int[] trail, IdDistMap[,] dists)
    {
        // total length of a trail
        var result = 0.0;
        for (var i = 0; i <= trail.Length - 2; i++)
        {
            result += Distance(trail[i], trail[i + 1], dists).Dist;
        }
        return result;
    }

    private IdDistMap[,] MakeGraphDistances(int numCities, Edge[] edges)
    {
        var dists = new IdDistMap[numCities, numCities];

        for (var i = 0; i < numCities; i++)
        {
            var fromCoords = edges[i].From;
            var toCoords = edges.Where(it => it.From.Id == fromCoords.Id).Select(it => it.To).ToList();
            
            for (var j = 0; j < toCoords.Count; j++)
            {
                var dist = MathExtensions.CalculateDistance(fromCoords.Point, toCoords[j].Point);
                var saveValueij = new IdDistMap
                {
                    FromId = fromCoords.Id,
                    ToId = toCoords[j].Id,
                    Dist = dist,
                };
                
                var saveValueji = new IdDistMap
                {
                    FromId = toCoords[j].Id,
                    ToId = fromCoords.Id,
                    Dist = dist,
                };


                dists[i, j] = saveValueij;
                dists[j, i] = saveValueji;
            }
        }
        
        return dists;
    }
}