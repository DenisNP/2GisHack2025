using GraphGeneration.Models;

namespace GraphGeneration.Geometry;

/// <summary>
/// Методы расширения для GeomPoint
/// </summary>
public static class GeomPointExtensions
{
    /// <summary>
    /// Вычисляет евклидово расстояние между двумя точками
    /// </summary>
    /// <param name="point1">Первая точка</param>
    /// <param name="point2">Вторая точка</param>
    /// <returns>Расстояние между точками</returns>
    public static double DistanceTo(this GeomPoint point1, GeomPoint point2)
    {
        if (point1 == null)
            throw new ArgumentNullException(nameof(point1));
        
        if (point2 == null)
            throw new ArgumentNullException(nameof(point2));

        double dx = point1.X - point2.X;
        double dy = point1.Y - point2.Y;
        
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Вычисляет квадрат расстояния между двумя точками (более быстрая операция для сравнения)
    /// </summary>
    /// <param name="point1">Первая точка</param>
    /// <param name="point2">Вторая точка</param>
    /// <returns>Квадрат расстояния между точками</returns>
    public static double DistanceSquaredTo(this GeomPoint point1, GeomPoint point2)
    {
        if (point1 == null)
            throw new ArgumentNullException(nameof(point1));
        
        if (point2 == null)
            throw new ArgumentNullException(nameof(point2));

        double dx = point1.X - point2.X;
        double dy = point1.Y - point2.Y;
        
        return dx * dx + dy * dy;
    }
}
