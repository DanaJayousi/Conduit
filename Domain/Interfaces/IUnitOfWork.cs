using Domain.Article;
using Domain.Comment;
using Domain.User;

namespace Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IArticleRepository _articleRepository { get; set; }
    ICommentRepository _commentRepository { get; set; }
    IUserRepository _userRepository { get; set; }
    Task<bool> Commit();
}