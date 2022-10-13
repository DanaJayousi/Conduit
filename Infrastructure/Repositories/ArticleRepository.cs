using Domain.Article;
using Domain.FavoriteArticle;
using Domain.User;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ArticleRepository : Repository<Article>, IArticleRepository
{
    public ArticleRepository(ConduitDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Article>> GetFeedAsync(int userId, int pageIndex, int pageSize)
    {
        var currentUser = await Context.Set<User>().FindAsync(userId);
        var currentUserFollowing = currentUser.Following.Select(userToUser => userToUser.UserId).ToList();
        return await Context.Set<Article>()
            .Where(article => currentUserFollowing.Contains(article.Author.Id))
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .OrderByDescending(article => article.LastUpdated)
            .ToListAsync();
    }

    public void FavoriteArticle(User user, Article article)
    {
        user.FavoriteArticles.Add(new FavoriteArticle
        {
            User = user,
            UserId = user.Id,
            Article = article,
            ArticleId = article.Id
        });
    }

    public void UnFavoriteArticle(User user, Article article)
    {
        var link = Context.Set<FavoriteArticle>()
            .SingleOrDefault(favoriteArticle =>
                favoriteArticle.UserId == user.Id && favoriteArticle.ArticleId == article.Id);
        Context.Set<FavoriteArticle>().Remove(link);
    }

    public Task<Article?> GetArticleWithoutCommentsAsync(int articleId)
    {
        return Context.Set<Article>()
            .Include(article => article.Author)
            .Include(article => article.FavoriteArticle)
            .FirstOrDefaultAsync(article => article.Id == articleId);
    }
}