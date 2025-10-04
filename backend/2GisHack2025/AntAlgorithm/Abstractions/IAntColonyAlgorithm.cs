namespace AntAlgorithm.Abstractions;

public interface IAntColonyAlgorithm
{
    /// <summary>
    /// Получить все пути с информацией о "весе" ребра и переходах между вершинами
    /// </summary>
    /// <returns></returns>
    List<Result> GetAllWays(Poi[] points);
    
    /// <summary>
    /// Получить самый лучший путь. Не имеет в себе информацию о "весах" рёбер
    /// </summary>
    /// <returns>Path <inheritdoc cref="Path"/>></returns>
    Path GetBestWay(Poi[] points);
}