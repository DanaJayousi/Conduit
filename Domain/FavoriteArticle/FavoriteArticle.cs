namespace Domain.FavoriteArticle;

public class FavoriteArticle
{
    public User.User User { get; set; }
    public int UserId { get; set; }
    public Article.Article Article { get; set; }
    public int ArticleId { get; set; }
}