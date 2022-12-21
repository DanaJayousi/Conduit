using Domain.User;
using Domain.UserToUser;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ConduitDbContext context) : base(context)
    {
    }

    public void Follow(User user, User follower)
    {
        if (follower.Following.SingleOrDefault(uTu => uTu.UserId == user.Id) != null) return;
        var link = new UserToUser
        {
            User = user,
            UserId = user.Id,
            Follower = follower,
            FollowerId = follower.Id
        };
        user.Followers.Add(link);
    }

    public void Unfollow(User user, User follower)
    {
        var link = follower.Following.SingleOrDefault(uTu => uTu.UserId == user.Id);
        if (link == null) return;
        follower.Following.Remove(link);
    }

    public Task<User?> GetUserWithFollowAsync(int userId)
    {
        return Context.Set<User>()
            .Include(user => user.Followers)
            .Include(user => user.Following)
            .FirstOrDefaultAsync(user => user.Id == userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await Context.Set<User>().Where(user => user.Email == email).SingleOrDefaultAsync();
    }
}