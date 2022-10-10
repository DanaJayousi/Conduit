namespace Domain.UserToUser;

public class UserToUser
{
    public User.User User { get; set; }
    public int UserId { get; set; }
    public User.User Follower { get; set; }
    public int FollowerId { get; set; }
}