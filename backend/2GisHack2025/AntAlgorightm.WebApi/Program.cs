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

app.MapPost("/getAllWays", ([FromBody]Poi[] points, IAntColonyAlgorithm algorithm) => GetAllWays(points, algorithm));
app.MapPost("/getBestPath", ([FromBody]Poi[] points, IAntColonyAlgorithm algorithm) => GetBestPath(points, algorithm));
app.UseCors();
app.Run();


List<Result> GetAllWays(Poi[] points, IAntColonyAlgorithm algorithm)
{
    return algorithm.GetAllWays(points);
}

Path GetBestPath(Poi[] points, IAntColonyAlgorithm algorithm)
{
    return algorithm.GetBestWay(points);
}
