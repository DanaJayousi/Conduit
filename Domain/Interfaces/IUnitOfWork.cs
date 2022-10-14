using Domain.Article;
using Domain.User;

namespace Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IArticleRepository _articleRepository { get; set; }
    IUserRepository _userRepository { get; set; }
    Task<bool> Commit();
}