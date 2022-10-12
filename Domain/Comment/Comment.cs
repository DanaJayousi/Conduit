namespace Domain.Comment;

public class Comment
{
    public int Id { get; set; }
    public User.User Author { get; set; }
    public int AuthorId { get; set; }
    public Article.Article Article { get; set; }
    public int ArticleId { get; set; }
    public string Content { get; set; }
    public DateTime PublishDate { get; set; }
}