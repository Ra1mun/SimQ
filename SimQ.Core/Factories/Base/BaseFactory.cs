using System.Reflection;

namespace SimQ.Core.Factories.Base;

public interface IFactory<out T>
    where T : class
{
    T? TryToCreate(string reflectionType, params object?[] args);
    bool Contains(string reflectionType);
}

public abstract class BaseFactory<T> : IFactory<T> where T : class
{
    protected readonly Dictionary<string, Type> DictionaryTypes;

    protected BaseFactory()
    {
        DictionaryTypes = GetAgentTypesDictionary();
    }

    public T? TryToCreate(string reflectionType, params object?[] args)
    {
        if (!Contains(reflectionType)) 
            return null;

        return CreateAgent(reflectionType, args);
    }

    public bool Contains(string reflectionType)
    {
        return DictionaryTypes.ContainsKey(reflectionType);
    }
    
    private Dictionary<string, Type> GetAgentTypesDictionary()
    {
        var baseType = typeof(T);
        var assembly = Assembly.GetAssembly(baseType);

        var types = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && baseType.IsAssignableFrom(t));

        return types.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
    }
    
    protected abstract T CreateAgent(string reflectionType, params object?[] args);
}