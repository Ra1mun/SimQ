using System.Linq.Expressions;

namespace SimQ.Domain.Models.Base;

public class BaseRequest<T>
{
    public Expression<Func<T, bool>> Predicate { get; set; }
}