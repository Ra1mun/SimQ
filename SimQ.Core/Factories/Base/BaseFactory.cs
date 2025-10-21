using System.Reflection;
using SimQ.Domain.Models.ProblemAggregation;

namespace SimQ.Core.Factories.Base;

public abstract class BaseFactory<T> 
    where T : class
{
    protected readonly Dictionary<string, Type> DictionaryTypes;

    protected BaseFactory()
    {
        DictionaryTypes = GetAgentTypesDictionary();
    }

    public T? TryToCreate(string reflectionType, IProblemParams? args)
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
    
    protected abstract T? CreateAgent(string reflectionType, IProblemParams? args);
}