using System.Collections.Generic;
using AntAlgorithm;
using AntAlgorithm.Abstractions;
using GraphGeneration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VoronatorSharp;
using WebApplication2;
using Path = AntAlgorithm.Path;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    // Сделал так, чтобы не думать про Cors
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services.AddAntColonyAlgorithm();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "My API V1");
    });
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors(policyBuilder => policyBuilder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.UseHttpsRedirection();

app.MapGet("/check", context => context.Response.WriteAsync("It works"));
app.MapPost("/getAllWays", ([FromBody]Edge[] edges, IAntColonyAlgorithm algorithm) => GetAllWays(edges, algorithm));
app.MapPost("/getBestPath3", ([FromBody]InputData data, AntColonyAlgorithm2 algorithm) => GraphGen.GetBestPath(data.Zones, data.Pois, algorithm));
app.MapPost("/runSimulation", ([FromBody]InputData data, AntColonyAlgorithm2 algorithm) => GraphGen.GetBestPath(data.Zones, data.Pois, algorithm));

app.UseCors();
app.Run();


List<Result> GetAllWays(Edge[] edges, IAntColonyAlgorithm algorithm)
{
    return algorithm.GetAllWays(edges);
}

Path GetBestPath(Edge[] edges, IAntColonyAlgorithm algorithm)
{
    return algorithm.GetBestWay(edges);
}

Path GetBestPath2(IAntColonyAlgorithm algorithm)
{
    var polygons = new List<ZonePolygon>
    {
        new ZonePolygon([
            new Vector2(0, 0),
            new Vector2(30, 0),
            new Vector2(30, 30),
            new Vector2(0, 30),
            new Vector2(0, 0)
        ]),
        new ZonePolygon([
            new Vector2(30, 10),  // Вплотную к первому полигону
            new Vector2(50, 10),
            new Vector2(40, 30),
            new Vector2(30, 30),
            new Vector2(30, 10)
        ]),
        new ZonePolygon([
            new Vector2(10, 30),  // Вплотную к первому полигону
            new Vector2(40, 30),
            new Vector2(40, 50),
            new Vector2(10, 50),
            new Vector2(10, 30)
        ])
    };

    List<Vector2> pois = [new Vector2(10001, 1, 2, 1), new Vector2(10002, 39, 18, 0.5)];

    var result = GraphGenerator.GenerateEdges(polygons, pois);
    
    return new Path();
}