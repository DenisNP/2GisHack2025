using NetTopologySuite.Geometries;

namespace GraphGeneration.Models;

public record PolygonMap(
    IReadOnlyCollection<ZonePolygon> Zones,
    IReadOnlyCollection<ZonePolygon> Generation,
    IReadOnlyCollection<Polygon> Urban,
    IReadOnlyCollection<Polygon> Available,
    IReadOnlyCollection<Polygon> Restricted,
    IReadOnlyCollection<Polygon> Render
    );
