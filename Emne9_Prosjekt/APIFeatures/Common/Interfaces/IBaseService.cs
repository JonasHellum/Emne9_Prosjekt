namespace Emne9_Prosjekt.Features.Common.Interfaces;

public interface IBaseService<T> where T : class
{
    Task<bool> DeleteByIdAsync(Guid id);
    Task<T?> GetByIdAsync(Guid memberId);
}