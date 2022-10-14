using Domain.Interfaces;

namespace Domain.Article;

public interface IArticleRepository : IRepository<Article>
{
    Task<IEnumerable<Article>> GetFeedAsync(User.User currentUser, int pageIndex, int pageSize);
    void FavoriteArticle(User.User user, Article article);
    void UnFavoriteArticle(User.User user, Article article);
    Task<Article?> GetArticleWithoutCommentsAsync(int articleId);
    Task<Article?> GetArticleWithCommentsAsync(int articleId);
    Task<Comment.Comment?> GetCommentByIdAsync(int articleId, int commentId);
    Task<IEnumerable<Comment.Comment?>> GetCommentsAsync(int articleId);
    void DeleteComment(Comment.Comment comment);
}