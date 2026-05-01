using LMS.Application.Common.Abstractions;
using LMS.Infrastructure.Persistence;

namespace LMS.Infrastructure.Services;

public sealed class UnitOfWork(LMSDbContext ctx) : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repositories = new();

    public IRepository<T> Repository<T>() where T : class
    {
        if (_repositories.TryGetValue(typeof(T), out var repository)) return (IRepository<T>)repository;
        var created = new GenericRepository<T>(ctx);
        _repositories[typeof(T)] = created;
        return created;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return ctx.SaveChangesAsync(cancellationToken);
    }
}