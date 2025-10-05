namespace AntAlgorithm;

/// <summary>
/// Конфигурация для алгоритма
/// https://en.wikipedia.org/wiki/Ant_colony_optimization_algorithms#:~:text=repeat%0Aend%20procedure-,Edge%20selection,-%5Bedit%5D
/// </summary>
public class AntColonyConfiguration
{
    /// <summary>
    /// Влияние феромона
    /// </summary>
    public double Alpha { get; set; } = 2.0;
    
    /// <summary>
    /// Влияние расстояния
    /// </summary>
    public double Beta { get; set; } = 20.0;

    /// <summary>
    /// Влияние веса вершины
    /// </summary>
    public double Gamma { get; set; } = 3.0;

    /// <summary>
    /// Испарение феромона
    /// </summary>
    public double Evaporation { get; set; } = 0.01;
    
    /// <summary>
    /// Константа для обновления феромона
    /// </summary>
    public double Q { get; set; } = 4;
    
    /// <summary>
    /// Количество итераций для поиска оптимального маршрута
    /// </summary>
    public int MaxIterations { get; set; } = 5000;
}