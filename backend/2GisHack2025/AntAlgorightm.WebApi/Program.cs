using AntAlgorithm;
using AntAlgorithm.Abstractions;
using Microsoft.AspNetCore.Mvc;
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
    app.UseCors("AllowAll");
}

app.UseHttpsRedirection();

app.MapPost("/getAllWays", ([FromBody]Edge[] edges, IAntColonyAlgorithm algorithm) => GetAllWays(edges, algorithm));
app.MapPost("/getBestPath", ([FromBody]Edge[] edges, IAntColonyAlgorithm algorithm) => GetBestPath(edges, algorithm));
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