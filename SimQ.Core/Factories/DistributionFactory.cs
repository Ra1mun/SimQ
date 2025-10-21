using System.Reflection;
using SimQ.Core.Factories.Base;
using SimQ.Domain.Models.ProblemAggregation;
using SimQCore.Library.Distributions;

namespace SimQ.Core.Factories;

public class DistributionFactory : BaseFactory<IDistribution>
{
    protected override IDistribution? CreateAgent(string typeName, IProblemParams? args)
    {
        if (!DictionaryTypes.TryGetValue(typeName, out var type))
            throw new ArgumentException($"Unknown agent type: {typeName}");
        
        if(args is null)
            return Activator.CreateInstance(type) as IDistribution;
        
        if(args is not DistributionParams distributionParams)
            throw new ArgumentException($"Unknown agent type: {typeName}");
        
        var matchingConstructor = FindMatchingConstructor(type, distributionParams.Arguments.ToArray());
        if (matchingConstructor == null)
            throw new ArgumentException(
                $"No suitable constructor found for type '{typeName}' with given arguments.");

        var convertedArgs = ConvertArguments(matchingConstructor.GetParameters(), args.Arguments.ToArray());
        return Activator.CreateInstance(type, convertedArgs) as IDistribution;
    }

    private static ConstructorInfo? FindMatchingConstructor(Type type, object?[] args)
    {
        return type.GetConstructors()
            .Where(ctor => ctor.GetParameters().Length == args.Length)
            .FirstOrDefault(ctor => ArgumentsMatchParameters(args, ctor.GetParameters()));
    }

    private static bool ArgumentsMatchParameters(object?[] args, ParameterInfo[] parameters)
    {
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var paramType = parameters[i].ParameterType;

            if (arg == null)
            {
                if (paramType.IsValueType && Nullable.GetUnderlyingType(paramType) == null)
                    return false;
            }
            else if (!(arg.GetType() == paramType ||
                       paramType.IsAssignableFrom(arg.GetType()) ||
                       CanConvert(arg, paramType)))
            {
                return false;
            }
        }

        return true;
    }

    private static object?[] ConvertArguments(ParameterInfo[] parameters, object?[] args)
    {
        var converted = new object?[args.Length];
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var paramType = parameters[i].ParameterType;

            if (arg == null)
                converted[i] = null;
            else if (arg.GetType() == paramType || paramType.IsAssignableFrom(arg.GetType()))
                converted[i] = arg;
            else
                converted[i] = ConvertArgument(arg, paramType);
        }

        return converted;
    }

    private static bool CanConvert(object arg, Type targetType)
    {
        return TryConvert(arg, targetType, out _);
    }

    private static object? ConvertArgument(object arg, Type targetType)
    {
        return TryConvert(arg, targetType, out var result)
            ? result
            : throw new InvalidOperationException(
                $"Failed to convert argument of type {arg.GetType()} to {targetType}.");
    }

    private static bool TryConvert(object value, Type targetType, out object? result)
    {
        result = null;

        try
        {
            if (targetType.IsEnum)
            {
                if (value is string s)
                {
                    result = Enum.Parse(targetType, s, true);
                    return true;
                }
            }
            else if (value is IConvertible)
            {
                result = Convert.ChangeType(value, targetType);
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}