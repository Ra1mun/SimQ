using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using SimQ.DAL.Repository;

namespace SimQ.DAL;

public static class DependencyInjection
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var dbSection = configuration.GetSection("DatabaseSettings");
        var connectionString = dbSection["ConnectionString"] ?? throw new InvalidOperationException("ConnectionString не задан");
        var databaseName = dbSection["DatabaseName"] ?? throw new InvalidOperationException("DatabaseName не задан");
    
        var mongoClient = new MongoClient(connectionString);
        var mongoDatabase = mongoClient.GetDatabase(databaseName);

        services.AddSingleton(mongoDatabase);
        
        return services;
    }
    
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IProblemRepository, ProblemRepository>();
        services.AddScoped<IResultRepository, ResultRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        
        return services;
    }
}