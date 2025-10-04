using System;
using System.Collections.Generic;
using System.Linq;
using Geometry;
using Xunit;

namespace Geometries.Tests;

public class GraphEnrichmentTests
{
    [Fact]
    public void TestPointInPolygon_ConvexSquare()
    {
        // Arrange
        var polygon = new List<Point>
        {
            new(0, 0),
            new(10, 0),
            new(10, 10),
            new(0, 10)
        };

        // Act & Assert
        Assert.True(GeometryUtils.IsPointInPolygon(new Point(5, 5), polygon));
        Assert.True(GeometryUtils.IsPointInPolygon(new Point(1, 1), polygon));
        Assert.False(GeometryUtils.IsPointInPolygon(new Point(15, 5), polygon));
        Assert.False(GeometryUtils.IsPointInPolygon(new Point(5, 15), polygon));
    }

    [Fact]
    public void TestPointInPolygon_ConcaveShape()
    {
        // Arrange - L-образный многоугольник
        var polygon = new List<Point>
        {
            new(0, 0),
            new(10, 0),
            new(10, 5),
            new(5, 5),
            new(5, 10),
            new(0, 10)
        };

        // Act & Assert
        Assert.True(GeometryUtils.IsPointInPolygon(new Point(2, 2), polygon));  // Внутри
        Assert.True(GeometryUtils.IsPointInPolygon(new Point(7, 2), polygon));  // Внутри
        Assert.False(GeometryUtils.IsPointInPolygon(new Point(7, 7), polygon)); // В вырезе
        Assert.False(GeometryUtils.IsPointInPolygon(new Point(12, 2), polygon)); // Снаружи
    }

    [Fact]
    public void TestBoundingBox()
    {
        // Arrange
        var polygon = new List<Point>
        {
            new(1, 2),
            new(5, 1),
            new(3, 6),
            new(0, 4)
        };

        // Act
        var (min, max) = GeometryUtils.GetBoundingBox(polygon);

        // Assert
        Assert.Equal(0, min.X);
        Assert.Equal(1, min.Y);
        Assert.Equal(5, max.X);
        Assert.Equal(6, max.Y);
    }

    [Fact]
    public void TestGridGeneration()
    {
        // Arrange
        var min = new Point(0, 0);
        var max = new Point(5, 5);
        double step = 2;

        // Act
        var points = GeometryUtils.GenerateGridPoints(min, max, step);

        // Assert
        Assert.Contains(points, p => p.X == 0 && p.Y == 0);
        Assert.Contains(points, p => p.X == 2 && p.Y == 2);
        Assert.Contains(points, p => p.X == 4 && p.Y == 4);
        Assert.DoesNotContain(points, p => p.X == 6 && p.Y == 6);
    }

    [Fact]
    public void TestGraphEnrichment_UrbanZone()
    {
        // Arrange
        var config = new GraphEnrichmentConfig { GridStep = 10, MaxPointsPerZone = 50 };
        var service = new GraphEnrichmentService(config);
        
        var zones = new List<Zone>
        {
            new Zone 
            { 
                Id = 1, 
                Type = ZoneType.Urban,
                Region = new List<Point>
                {
                    new(0, 0),
                    new(20, 0),
                    new(20, 20),
                    new(0, 20)
                }
            }
        };

        var pois = new List<Poi>
        {
            new Poi { Id = 1, Point = new Point(5, 5), Weight = 1.0 }
        };

        // Act
        var result = service.EnrichGraph(zones, pois);

        // Assert
        Assert.True(result.Count > 1); // Должны быть POI + сгенерированные точки
        Assert.Contains(result, p => p.X == 5 && p.Y == 5); // POI точка должна быть
        Assert.Contains(result, p => p.X == 0 && p.Y == 0); // Угловая точка зоны
    }

    [Fact]
    public void TestGraphEnrichment_RestrictedZoneFiltering()
    {
        // Arrange
        var service = new GraphEnrichmentService();
        
        var zones = new List<Zone>
        {
            new Zone 
            { 
                Id = 1, 
                Type = ZoneType.Urban,
                Region = new List<Point>
                {
                    new(0, 0),
                    new(30, 0),
                    new(30, 30),
                    new(0, 30)
                }
            },
            new Zone 
            { 
                Id = 2, 
                Type = ZoneType.Restricted,
                Region = new List<Point>
                {
                    new(10, 10),
                    new(20, 10),
                    new(20, 20),
                    new(10, 20)
                }
            }
        };

        var pois = new List<Poi>();

        // Act
        var result = service.EnrichGraph(zones, pois);

        // Assert
        // Не должно быть точек внутри restricted зоны
        Assert.DoesNotContain(result, p => p.X > 10 && p.X < 20 && p.Y > 10 && p.Y < 20);
    }

    [Fact]
    public void TestEffectiveDistanceCalculation()
    {
        // Arrange
        var config = new GraphEnrichmentConfig { ImpatienceFactor = 0.5 };
        var service = new GraphEnrichmentService(config);
        var a = new Point(0, 0);
        var b = new Point(10, 0);

        // Act
        var urbanDistance = service.CalculateEffectiveDistance(a, b, ZoneType.Urban);
        var availableDistance = service.CalculateEffectiveDistance(a, b, ZoneType.Available);

        // Assert
        Assert.Equal(5.0, urbanDistance); // 10 * 0.5
        Assert.Equal(10.0, availableDistance); // 10 * 1.0
    }

    [Fact]
    public void TestPoiSelectionByWeight()
    {
        // Arrange
        var service = new GraphEnrichmentService();
        var pois = new List<Poi>
        {
            new Poi { Id = 1, Weight = 0.5 },
            new Poi { Id = 2, Weight = 1.0 },
            new Poi { Id = 3, Weight = 0.2 }
        };

        // Act - многократный выбор для проверки распределения
        var selections = new List<int>();
        var random = new Random(42); // Фиксированный seed для воспроизводимости
        
        for (int i = 0; i < 1000; i++)
        {
            var selected = service.SelectPoiByWeight(pois, random);
            selections.Add(selected.Id);
        }

        var count1 = selections.Count(id => id == 1);
        var count2 = selections.Count(id => id == 2);
        var count3 = selections.Count(id => id == 3);

        // Assert - проверяем примерное распределение
        // POI2 должен выбираться примерно в 2 раза чаще чем POI1
        Assert.True(count2 > count1);
        Assert.True(count1 > count3);
    }

    [Fact]
    public void TestDelaunayPointGeneration()
    {
        // Arrange
        var polygon = new List<Point>
        {
            new(0, 0),
            new(10, 0),
            new(10, 10),
            new(0, 10)
        };

        // Act
        var points = GeometryUtils.GenerateDelaunayPoints(polygon, 2.0);

        // Assert
        Assert.True(points.Count >= polygon.Count); // Должны быть вершины + дополнительные точки
        Assert.All(points, point => 
            Assert.True(GeometryUtils.IsPointInPolygon(point, polygon))); // Все точки внутри полигона
    }
}