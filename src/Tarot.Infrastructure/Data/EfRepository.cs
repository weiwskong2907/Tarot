using Microsoft.EntityFrameworkCore;
using Tarot.Core.Interfaces;

namespace Tarot.Infrastructure.Data;

public class EfRepository<T>(AppDbContext dbContext) : IRepository<T> where T : class
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Set<T>().FindAsync(id);
    }

    public async Task<IReadOnlyList<T>> ListAllAsync()
    {
        return await _dbContext.Set<T>().ToListAsync();
    }

    public async Task<IReadOnlyList<T>> ListAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
    {
        return await _dbContext.Set<T>().Where(predicate).ToListAsync();
    }

    public async Task<IReadOnlyList<T>> ListAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, int skip, int take)
    {
        return await _dbContext.Set<T>().Where(predicate).Skip(skip).Take(take).ToListAsync();
    }

    public async Task<T?> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
    {
        return await _dbContext.Set<T>().FirstOrDefaultAsync(predicate);
    }

    public async Task<int> CountAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
    {
        return await _dbContext.Set<T>().CountAsync(predicate);
    }

    public async Task<T> AddAsync(T entity)
    {
        await _dbContext.Set<T>().AddAsync(entity);
        await _dbContext.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(T entity)
    {
        _dbContext.Entry(entity).State = EntityState.Modified;
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        var prop = typeof(T).GetProperty("DeletedAt");
        if (prop != null && prop.PropertyType == typeof(DateTimeOffset?))
        {
            prop.SetValue(entity, DateTimeOffset.UtcNow);
            _dbContext.Entry(entity).State = EntityState.Modified;
        }
        else
        {
            _dbContext.Set<T>().Remove(entity);
        }
        await _dbContext.SaveChangesAsync();
    }
}
