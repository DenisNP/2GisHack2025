namespace AntAlgorithm;

public class Edge
{
    public Edge() {}
    public Edge(Poi From , Poi To )
    {
        this.From = From;
        this.To = To;
    }
    public Poi From { get; set; }
    public Poi To { get; set; }
}