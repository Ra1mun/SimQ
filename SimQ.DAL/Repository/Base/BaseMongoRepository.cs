using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SimQ.Domain.Models.Base;
using SimQ.Domain.Models.DBSettings;

namespace SimQ.DAL.Repository.Base;

public interface IBaseRepository<T> 
    where T : IMongoObjectEntity
{
    T? Get(BaseRequest<T> request);
    List<T>? GetMany(BaseRequest<T> request);
    Task<T?> GetAsync(BaseRequest<T> request, CancellationToken cancellationToken = default);
    Task<List<T>?> GetManyAsync(BaseRequest<T> request, CancellationToken cancellationToken = default);

    Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<T> AddAsync(T result, CancellationToken cancellationToken = default);
    
    Task<bool> UpdateAsync(string id, T newEntity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<bool> ExistProblemAsync(string id, CancellationToken cancellationToken = default);
}

internal abstract class BaseMongoRepository<T> : IBaseRepository<T>
    where T : IMongoObjectEntity
{
    protected readonly IMongoCollection<T> Collection;
    protected abstract string CollectionName { get; set; }
    
    protected BaseMongoRepository(IOptions<DatabaseSettings> options)
    {
        var settings = options.Value;
        var client = new MongoClient(settings.ConnectionString);
        var database = client.GetDatabase(settings.DatabaseName);

        Collection = database.GetCollection<T>(CollectionName);
    }

    public T? Get(BaseRequest<T> request)
    {
        return Collection.Find(request.Predicate).FirstOrDefault();
    }

    public List<T>? GetMany(BaseRequest<T> request)
    {
        return Collection.Find(request.Predicate).ToList();
    }

    public async Task<T?> GetAsync(BaseRequest<T> request, CancellationToken cancellationToken = default)
    {
        return await Collection.Find(request.Predicate).FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }

    public async Task<List<T>?> GetManyAsync(BaseRequest<T> request, CancellationToken cancellationToken = default)
    {
        return await Collection.Find(request.Predicate).ToListAsync(cancellationToken: cancellationToken);
    }
    
    public async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Collection.Find(_ => true).ToListAsync(cancellationToken: cancellationToken);
    }
    
    public async Task<T> AddAsync(T result, CancellationToken cancellationToken = default)
    {   
        await Collection.InsertOneAsync(result, cancellationToken: cancellationToken);
        
        return result;
    }
    
    public async Task<bool> UpdateAsync(string id, T newProblem, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.Eq(p => p.Id, id);
        
        var result = await Collection.ReplaceOneAsync(filter, newProblem, cancellationToken: cancellationToken);
        
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await Collection.DeleteOneAsync(r => r.Id == id, cancellationToken);
        
        return result.DeletedCount > 0;
    }
    
    public async Task<bool> ExistProblemAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Collection.Find(Builders<T>.Filter.Eq(p => p.Id, id)).AnyAsync();
    }
}