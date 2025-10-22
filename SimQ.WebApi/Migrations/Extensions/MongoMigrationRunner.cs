using System.Reflection;
using MongoDB.Driver;
using SimQ.Core.Exceptions;
using SimQ.Domain.Models.Migration;

namespace SimQ.WebApi.Migrations.Extensions;

public static class MongoMigrationRunner
{
    public static async Task RunMigrationsAsync(
        IMongoDatabase database,
        Assembly assembly,
        ILogger logger)
    {
        var migrations = GetMigrations(assembly);
        if (migrations == null)
        {
            logger.LogInformation("Миграций не найдено.");
            return;
        }

        var maxVersion = migrations.Max(m => m.Version);
        logger.LogInformation("Найдено {Count} миграций. Максимальная версия: {MaxVersion}", migrations.Count, maxVersion);

        var migrationCollection = database.GetCollection<MigrationRecord>("__migrations");
        var applied = (await migrationCollection.Find(_ => true).ToListAsync())
            .Select(r => r.Version)
            .ToHashSet();

        foreach (var migration in migrations)
        {
            if (applied.Contains(migration.Version))
            {
                logger.LogDebug("Миграция v{Version} уже применена", migration.Version);
                continue;
            }

            logger.LogInformation("Применяется миграция v{Version}", migration.Version);
            try
            {
                await migration.ApplyAsync(database);
            }
            catch
            {
                throw new MigrationException("Не получилось применить миграцию откатываем изменения.");
            }

            await migrationCollection.InsertOneAsync(new MigrationRecord
            {
                Version = migration.Version,
                Name = migration.GetType().Name,
                AppliedAt = DateTime.UtcNow
            });
            logger.LogInformation("Миграция v{Version} успешно применена", migration.Version);
        }
    }

    public static async Task RollbackMigrationAsync(IMongoDatabase database,
        Assembly assembly)
    {
        var migrations = GetMigrations(assembly);
        if (migrations == null)
        {
            throw new MigrationException("Не удалось найти миграций для отката");
        }
        
        var migration = migrations.FirstOrDefault(x => x.Version == migrations.Max(m => m.Version));
        
        await migration.RollbackAsync(database);
    }

    private static List<IMigration>? GetMigrations(Assembly assembly)
    {
        var migrationTypes = assembly
            .GetTypes()
            .Where(t => typeof(IMigration).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        if (migrationTypes.Count == 0)
        {
            return null;
        }

        return migrationTypes
            .Select(t => (IMigration)Activator.CreateInstance(t)!)
            .OrderBy(m => m.Version)
            .ToList();;
    }
}

