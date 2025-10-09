using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BankingSystem.Repository
{
    public class Repository<TEntity> where TEntity : class
    {
        private readonly BankingDbContext _db;
        private readonly DbSet<TEntity> _dbSet;

        public Repository(BankingDbContext db)
        {
            _db = db;
            _dbSet = _db.Set<TEntity>();
        }

        //public async Task<TEntity?> GetByIdAsync(Guid id) => await _dbSet.FindAsync(id);

        //public async Task<IEnumerable<TEntity>> GetAllAsync() => await _dbSet.ToListAsync();

        //public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate) =>
        //    await _dbSet.Where(predicate).ToListAsync();
        //public async Task AddAsync(TEntity entity) => await _dbSet.AddAsync(entity);
        //public void Update(TEntity entity) => _dbSet.Update(entity);
        //public void Remove(TEntity entity) => _dbSet.Remove(entity);
        //public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();

    }
}
