using System.Collections.Generic;
using and.Models;
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
app.MapPost("/getBestPath3", ([FromBody]InputData data, AntColonyAlgorithm2 algorithm) => GraphGen.GetBestPath(data.Zones, data.Pois, algorithm));
app.MapPost("/runSimulation", ([FromBody]InputData data, AntColonyAlgorithm2 algorithm) => GraphGen.GetBestPath(data.Zones, data.Pois, algorithm));

app.UseCors();
app.Run();