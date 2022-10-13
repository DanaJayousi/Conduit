using Domain.Interfaces;

namespace Domain.User;

public interface IUserRepository : IRepository<User>
{
    void Follow(User user, User follower);
    void Unfollow(User user, User follower);
    Task<User?> GetUserWithFollowAsync(int userId);
    Task<User?> GetUserWithArticlesAsync(int userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> ValidateUserCredentialsAsync(string email, string password);
}