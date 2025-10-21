using Microsoft.Extensions.DependencyInjection;
using SimQ.DAL.Repository;

namespace SimQ.DAL;

public static class DependencyInjection
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IProblemRepository, ProblemRepository>();
        services.AddScoped<IResultRepository, ResultRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        
        return services;
    }
}