using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SimQ.Core.Convertors.Agents;
using SimQ.Core.Convertors.Problem;
using SimQ.Core.MappingProfiles;
using SimQ.Core.Services;

namespace SimQ.Core;

public static class DependecyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IProblemService, ProblemService>();
        services.AddScoped<ITaskService, TaskService>();
        
        services.AddAutoMapper(typeof(AgentProfile).Assembly);
        services.AddAutoMapper(typeof(ResultProfile).Assembly);
        
        return services;
    }
    
    public static IServiceCollection AddConverters(this IServiceCollection services)
    {
        services.AddScoped<IAgentConverter, AgentConverter>();
        services.AddScoped<IBufferConverter, BufferConverter>();
        services.AddScoped<IServiceBlockConverter, ServiceBlockConverter>();
        services.AddScoped<ISourceConverter, SourceConverter>();
        services.AddScoped<IProblemConvertor, ProblemConvertor>();
        
        return services;
    }
}