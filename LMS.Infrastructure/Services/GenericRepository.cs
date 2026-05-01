using System.Linq.Expressions;
using LMS.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Services;

public sealed class GenericRepository<T>(DbContext dbContext) : IRepository<T> where T : class
{
    private readonly DbSet<T> _set = dbContext.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _set.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        return predicate is null
            ? await _set.ToListAsync(cancellationToken)
            : await _set.Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _set.AddAsync(entity, cancellationToken);
    }

    public void Update(T entity)
    {
        _set.Update(entity);
    }

    public void Remove(T entity)
    {
        _set.Remove(entity);
    }
}