namespace AntAlgorithm;

internal class MathExtensions
{
    public static double CalculateDistance(double x1, double y1, double x2, double y2)
    {
        var deltaX = x2 - x1;
        var deltaY = y2 - y1;
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }
    
    public static double CalculateDistance(Point from, Point to)
    {
        var deltaX = to.X - from.X;
        var deltaY = to.Y - from.Y;
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }
}