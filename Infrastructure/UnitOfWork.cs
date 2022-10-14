using Domain.Article;
using Domain.Interfaces;
using Domain.User;
using Infrastructure.Repositories;

namespace Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly ConduitDbContext _context;

    public UnitOfWork(ConduitDbContext context)
    {
        _context = context;
        _articleRepository = new ArticleRepository(_context);
        _userRepository = new UserRepository(_context);
    }

    public IArticleRepository _articleRepository { get; set; }
    public IUserRepository _userRepository { get; set; }

    public async Task<bool> Commit()
    {
        return await _context.SaveChangesAsync() >= 0;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}