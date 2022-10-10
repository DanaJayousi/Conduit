using Domain.Interfaces;

namespace Domain.Article;

public interface IArticleRepository : IRepository<Article>
{
    Task<IEnumerable<Article>> GetFeedAsync(int userId, int pageIndex, int pageSize);
    void FavoriteArticle(User.User user, Article article);
    void UnFavoriteArticle(User.User user, Article article);

}