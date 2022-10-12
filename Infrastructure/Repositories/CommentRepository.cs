using Domain.Comment;

namespace Infrastructure.Repositories;

public class CommentRepository : Repository<Comment>, ICommentRepository
{
    public CommentRepository(ConduitDbContext context) : base(context)
    {
    }
}