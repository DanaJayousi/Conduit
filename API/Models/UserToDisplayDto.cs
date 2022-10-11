namespace API.Models;

public class UserToDisplayDto
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
}