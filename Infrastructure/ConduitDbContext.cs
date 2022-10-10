using Domain.Article;
using Domain.Comment;
using Domain.FavoriteArticle;
using Domain.User;
using Domain.UserToUser;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class ConduitDbContext : DbContext
{
    public ConduitDbContext()
    {
    }

    public ConduitDbContext(DbContextOptions<ConduitDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Article> Articles { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<UserToUser> Follow { get; set; }
    public DbSet<FavoriteArticle> FavoriteArticles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>(
            entity => { entity.HasKey(user => user.Id); }
        );
        modelBuilder.Entity<Article>(
            entity =>
            {
                entity.HasKey(article => article.Id);
                entity.Property(article => article.LastUpdated)
                    .HasColumnType("datetime");
                entity.Property(article => article.PublishDate)
                    .HasColumnType("datetime");
            }
        );
        modelBuilder.Entity<Comment>(
            entity =>
            {
                entity.HasKey(comment => comment.Id);
                entity.HasOne(comment => comment.Article)
                    .WithMany(article => article.Comments)
                    .HasForeignKey(comment => comment.ArticleId);
                entity.HasOne(comment => comment.Author)
                    .WithMany(user => user.Comments)
                    .HasForeignKey(comment => comment.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.Property(comment => comment.PublishDate)
                    .HasColumnType("datetime");
            }
        );
        modelBuilder.Entity<UserToUser>(
            entity =>
            {
                entity.HasKey(userToUser => new { userToUser.UserId, userToUser.FollowerId });
                entity.HasOne(userToUser => userToUser.User)
                    .WithMany(user => user.Followers)
                    .HasForeignKey(userToUser => userToUser.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(userToUser => userToUser.Follower)
                    .WithMany(user => user.Following)
                    .HasForeignKey(userToUser => userToUser.FollowerId)
                    .OnDelete(DeleteBehavior.NoAction);
            }
        );
        modelBuilder.Entity<FavoriteArticle>(
            entity =>
            {
                entity.HasKey(article => new { article.UserId, article.ArticleId });
                entity.HasOne(article => article.Article)
                    .WithMany(article => article.FavoriteArticle)
                    .HasForeignKey(article => article.ArticleId);
                entity.HasOne(article => article.User)
                    .WithMany(user => user.FavoriteArticles)
                    .HasForeignKey(user => user.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            }
        );
    }
}