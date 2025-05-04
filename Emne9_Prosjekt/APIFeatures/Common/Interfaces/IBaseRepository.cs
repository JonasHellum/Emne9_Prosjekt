using System.Linq.Expressions;

namespace Emne9_Prosjekt.Features.Common.Interfaces;

public interface IBaseRepository<T> where T : class
{
    Task<T?> AddAsync(T entity);
    Task<T?> DeleteByIdAsync(Guid id);
    Task<T?> UpdateAsync(T entity); 
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate );
    
}