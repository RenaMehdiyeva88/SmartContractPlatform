using System.Linq.Expressions;
using SmartContractPlatform.DAL.Entities;

namespace SmartContractPlatform.DAL.Repositories.Interfaces;

public interface IGenericRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    void Update(T entity);

    /// <summary>Soft-delete: IsDeleted = true qoyur, sətri fiziki silmir</summary>
    void Remove(T entity);

    /// <summary>Fiziki silmə. Yalnız həqiqətən lazım olduğu yerlərdə (məs. cache/temp data) istifadə et</summary>
    void HardRemove(T entity);

    Task<int> SaveChangesAsync();
}