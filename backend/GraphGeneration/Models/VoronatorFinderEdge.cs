using VoronatorSharp;

namespace GraphGeneration.Models;

public class VoronatorFinderEdge : IEdge<Vector2>
{
    public VoronatorFinderEdge()
    {
    }

    public VoronatorFinderEdge(Vector2 v1, Vector2 v2)
    {
        Source = v1;
        Target = v2;
    }
    
    public IEnumerable<Vector2> Points => [Source, Target];
    
    public Vector2 Source { get; set; }
    public Vector2 Target { get; set; }
        
    public override string ToString() => $"{Source} -> {Target}";
        
    // Для сравнения рёбер (независимо от направления)
    public override bool Equals(object obj)
    {
        if (obj is VoronatorFinderEdge other)
        {
            return (Source.Equals(other.Source) && Target.Equals(other.Target)) ||
                   (Source.Equals(other.Target) && Target.Equals(other.Source));
        }
        return false;
    }
        
    public override int GetHashCode()
    {
        return Source.GetHashCode() ^ Target.GetHashCode();
    }
}