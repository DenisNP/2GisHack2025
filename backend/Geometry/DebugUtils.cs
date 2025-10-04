// DebugUtils.cs
namespace Geometry;

public static class DebugUtils
{
    public static void PrintPoints(List<Point> points, string title = "Points")
    {
        Console.WriteLine($"{title} (count: {points.Count}):");
        foreach (var point in points)
        {
            Console.WriteLine($"  ({point.X}, {point.Y})");
        }
    }

    public static void ValidatePointsInPolygon(List<Point> points, List<Point> polygon, string context = "")
    {
        var invalidPoints = points.Where(p => !GeometryUtils.IsPointInPolygon(p, polygon)).ToList();
        
        if (invalidPoints.Any())
        {
            Console.WriteLine($"Invalid points in {context}: {invalidPoints.Count}");
            foreach (var point in invalidPoints)
            {
                Console.WriteLine($"  ({point.X}, {point.Y})");
            }
        }
        else
        {
            Console.WriteLine($"All points are valid in {context}");
        }
    }
}