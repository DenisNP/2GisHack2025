using AntAlgorithm;
using AntAlgorithm.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebApplication2;
using WebApplication2.Dto;

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
app.MapPost("/getBestPath3", ([FromBody]InputData data, IAntColonyAlgorithm _) => PathScapeService.GenerateGraph(data.Zones, data.Pois));
app.MapPost("/runSimulation", ([FromBody]InputData data) => PathScapeService.RunSimulation(data.Zones, data.Pois));

app.UseCors();
app.Run();
