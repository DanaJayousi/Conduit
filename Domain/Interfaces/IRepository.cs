namespace Domain.Interfaces;

public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetAsync(int id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task Add(TEntity entity);
    void Remove(TEntity entity);
}