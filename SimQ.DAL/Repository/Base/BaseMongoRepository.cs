using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SimQ.Domain.Models.Base;
using SimQ.Domain.Models.DBSettings;

namespace SimQ.DAL.Repository;

public interface IBaseRepository<T> 
    where T : IMongoObjectEntity
{
    T? Get(BaseRequest<T> request);
    List<T>? GetMany(BaseRequest<T> request);
    Task<T?> GetAsync(BaseRequest<T> request);
    Task<List<T>?> GetManyAsync(BaseRequest<T> request);

    Task<List<T>> GetAllAsync();

    Task<T> AddAsync(T result);
    
    Task<bool> UpdateAsync(string id, T newEntity);
    Task<bool> DeleteAsync(string id);

    Task<bool> ExistProblemAsync(string id);
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

    public async Task<T?> GetAsync(BaseRequest<T> request)
    {
        return await Collection.Find(request.Predicate).FirstOrDefaultAsync();
    }

    public async Task<List<T>?> GetManyAsync(BaseRequest<T> request)
    {
        return await Collection.Find(request.Predicate).ToListAsync();
    }
    
    public async Task<List<T>> GetAllAsync()
    {
        return await Collection.Find(_ => true).ToListAsync();
    }
    
    public async Task<T> AddAsync(T result)
    {   
        await Collection.InsertOneAsync(result);
        
        return result;
    }
    
    public async Task<bool> UpdateAsync(string id, T newProblem)
    {
        var filter = Builders<T>.Filter.Eq(p => p.Id, id);
        
        var result = await Collection.ReplaceOneAsync(filter, newProblem);
        
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await Collection.DeleteOneAsync(r => r.Id == id);
        
        return result.DeletedCount > 0;
    }
    
    public async Task<bool> ExistProblemAsync(string id)
    {
        return await Collection.Find(Builders<T>.Filter.Eq(p => p.Id, id)).AnyAsync();
    }
}