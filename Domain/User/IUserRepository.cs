using Domain.Interfaces;

namespace Domain.User;

public interface IUserRepository : IRepository<User>
{
    Task FollowAsync(User user, User follower);
    void Unfollow(User user, User follower);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> ValidateUserCredentialsAsync(string email, string password);
}