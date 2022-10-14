using Domain.Article;
using Domain.Comment;
using Domain.FavoriteArticle;
using Domain.User;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ArticleRepository : Repository<Article>, IArticleRepository
{
    public ArticleRepository(ConduitDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Article>> GetFeedAsync(User currentUser, int pageIndex, int pageSize)
    {
        var currentUserFollowing = currentUser.Following.Select(userToUser => userToUser.UserId).ToList();
        return await Context.Set<Article>()
            .Where(article => currentUserFollowing.Contains(article.Author.Id))
            .Include(article => article.Author)
            .Include(article => article.FavoriteArticle)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .OrderByDescending(article => article.LastUpdated)
            .ToListAsync();
    }

    public void FavoriteArticle(User user, Article article)
    {
        if (user.FavoriteArticles.SingleOrDefault(favArticle => favArticle.ArticleId == article.Id) != null) return;
        var link = new FavoriteArticle
        {
            User = user,
            UserId = user.Id,
            Article = article,
            ArticleId = article.Id
        };
        user.FavoriteArticles.Add(link);
    }

    public void UnFavoriteArticle(User user, Article article)
    {
        var link = user.FavoriteArticles.SingleOrDefault(favArticle => favArticle.ArticleId == article.Id);
        if (link == null) return;
        user.FavoriteArticles.Remove(link);
    }

    public Task<Article?> GetArticleWithoutCommentsAsync(int articleId)
    {
        return Context.Set<Article>()
            .Include(article => article.Author)
            .Include(article => article.FavoriteArticle)
            .FirstOrDefaultAsync(article => article.Id == articleId);
    }

    public async Task<Article?> GetArticleWithCommentsAsync(int articleId)
    {
        return await Context.Set<Article>()
            .Include(article => article.Comments)
            .FirstOrDefaultAsync(article => article.Id == articleId);
    }

    public async Task<Comment?> GetCommentByIdAsync(int articleId, int commentId)
    {
        return await Context.Set<Comment>()
            .Include(comment => comment.Article)
            .Include(comment => comment.Author)
            .FirstOrDefaultAsync(comment => comment.Id == commentId && comment.Article.Id == articleId);
    }

    public async Task<IEnumerable<Comment?>> GetCommentsAsync(int articleId)
    {
        return await Context.Set<Comment>()
            .Include(comment => comment.Article)
            .Include(comment => comment.Author)
            .Where(comment => comment.Article.Id == articleId)
            .ToListAsync();
    }

    public void DeleteComment(Comment comment)
    {
        Context.Set<Comment>().Remove(comment);
    }
}