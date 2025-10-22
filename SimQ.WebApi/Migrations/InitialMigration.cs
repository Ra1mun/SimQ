using MongoDB.Driver;
using SimQ.Domain.Models.Migration;

namespace SimQ.WebApi.Migrations;

public class InitialMigration : IMigration
{
    public int Version { get; } = 1;
    public async Task ApplyAsync(IMongoDatabase database)
    {
        await database.CreateCollectionAsync("Problems");
        await database.CreateCollectionAsync("Results");
        await database.CreateCollectionAsync("Tasks");
    }

    public async Task RollbackAsync(IMongoDatabase database)
    {
        await database.DropCollectionAsync("Problems");
        await database.DropCollectionAsync("Results");
        await database.DropCollectionAsync("Tasks");
    }
}