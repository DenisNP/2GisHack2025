namespace AntAlgorithm;

public class Zone
{
    public int Id { get; set; }
    public IEnumerable<Point> Region { get; set; }
    public ZoneType ZoneType { get; set; }
}