namespace API.Models;

public class CommentToDisplayDto
{
    public int Id { get; set; }
    public string AuthorName { get; set; }
    public int AuthorId { get; set; }
    public string ArticleTitle { get; set; }
    public int ArticleId { get; set; }
    public string Content { get; set; }
    public string PublishDate { get; set; }
}