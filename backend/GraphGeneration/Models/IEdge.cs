namespace GraphGeneration.Models;

public interface IEdge<out TPoint>
{
    /// <summary>Gets the source vertex</summary>
    /// <getter>
    ///   <ensures>Contract.Result&lt;TVertex&gt;() != null</ensures>
    /// </getter>
    TPoint Source { get; }

    /// <summary>Gets the target vertex</summary>
    /// <getter>
    ///   <ensures>Contract.Result&lt;TVertex&gt;() != null</ensures>
    /// </getter>
    TPoint Target { get; }
}