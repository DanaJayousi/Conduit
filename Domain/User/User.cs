namespace Domain.User;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName => $"{FirstName}{LastName}";
    public List<Article.Article> Articles { get; set; } = new();
    public List<UserToUser.UserToUser> Following { get; set; } = new();
    public List<UserToUser.UserToUser> Followers { get; set; } = new();
    public List<FavoriteArticle.FavoriteArticle> FavoriteArticles { get; set; } = new();
    public List<Comment.Comment> Comments { get; set; } = new();
}