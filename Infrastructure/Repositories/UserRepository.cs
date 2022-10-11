using Domain.User;
using Domain.UserToUser;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ConduitDbContext context) : base(context)
    {
    }

    public async Task FollowAsync(User user, User follower)
    {
        await Context.Set<UserToUser>().AddAsync(new UserToUser
        {
            User = user,
            UserId = user.Id,
            Follower = follower,
            FollowerId = follower.Id
        });
    }

    public void Unfollow(User user, User follower)
    {
        var link = follower.Following.SingleOrDefault(uTu => uTu.UserId == user.Id);
        Context.Set<UserToUser>().Remove(link);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await Context.Set<User>().Where(user => user.Email == email).SingleOrDefaultAsync();
    }
}