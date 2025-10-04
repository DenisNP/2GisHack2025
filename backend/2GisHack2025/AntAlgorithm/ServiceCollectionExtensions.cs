using AntAlgorithm.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AntAlgorithm;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAntColonyAlgorithm(this IServiceCollection services)
    {
        services.AddOptions<AntColonyConfiguration>();
        services.AddTransient<IAntColonyAlgorithm, AntColonyAlgorithm>();
        
        return services;
    }
}