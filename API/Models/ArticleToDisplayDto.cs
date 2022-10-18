namespace API.Models;

public class ArticleToDisplayDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string AuthorName { get; set; }
    public string PublishDate { get; set; }
    public string LastUpdated { get; set; }
    public string Content { get; set; }
    public int FavoritedCount { get; set; }
}