namespace Domain.Article;

public class Article
{
    public int Id { get; set; }
    public string Title { get; set; }
    public User.User Author { get; set; }
    public DateTime PublishDate { get; set; }
    public DateTime LastUpdated { get; set; }
    public string Content { get; set; }
    public List<Comment.Comment> Comments { get; set; } = new();

    public int FavoritedCount => FavoriteArticle.Count();
    public List<FavoriteArticle.FavoriteArticle> FavoriteArticle { get; set; } = new();
}