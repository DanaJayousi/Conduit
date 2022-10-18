using System.Security.Claims;
using API.Controllers;
using API.Models;
using API.Profiles;
using AutoMapper;
using Domain.Article;
using Domain.FavoriteArticle;
using Domain.Interfaces;
using Domain.User;
using Domain.UserToUser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UnitTests;

public class ArticlesControllerTests
{
    private readonly Mock<IArticleRepository> _articleRepositoryMock = new();
    private readonly Mock<HttpContext> _contextMock = new();

    private readonly Mapper _mapper = new(new MapperConfiguration(cfg =>
        cfg.AddProfile(new ArticleProfile())));

    private readonly ArticlesController _sut;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ClaimsPrincipal> _userMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();

    public ArticlesControllerTests()
    {
        var controllerContext = new ControllerContext
        {
            HttpContext = _contextMock.Object
        };
        _sut = new ArticlesController(_mapper, _unitOfWorkMock.Object, _articleRepositoryMock.Object,
            _userRepositoryMock.Object)
        {
            ControllerContext = controllerContext
        };
    }

    [Fact]
    public async Task GetArticleById_ReturnsNotFound_WhenThereIsNoSuchArticle()
    {
        _articleRepositoryMock.Setup(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()))
            .ReturnsAsync(() => null);

        var result = await _sut.GetArticleById(0);

        _articleRepositoryMock.Verify(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()),
            Times.Once);
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetArticleById_ReturnsTheArticle_WhenArticleExist()
    {
        var article = new Article
        {
            Id = 5,
            Title = "title",
            Author = new User(),
            PublishDate = default,
            LastUpdated = default
        };
        var articleDto = _mapper.Map<ArticleToDisplayDto>(article);
        _articleRepositoryMock.Setup(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()))
            .ReturnsAsync(article);

        var result = await _sut.GetArticleById(5);

        _articleRepositoryMock.Verify(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()),
            Times.Once);
        result.Result.Should().BeOfType<OkObjectResult>();
        ((ObjectResult)result.Result).Value.Should().BeEquivalentTo(articleDto);
    }

    [Fact]
    public async Task AddArticle_ReturnsTheAddedArticle()
    {
        var userId = 5;
        var claim = new Claim("userId", userId.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        var articleToInsert = new ArticleToUpsertDto
        {
            Title = "title",
            Content = "Content"
        };
        Article article = null;
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _userRepositoryMock.Setup(repository => repository.GetUserWithArticlesAsync(It.IsAny<int>()))
            .ReturnsAsync(new User { Id = userId });
        _articleRepositoryMock.Setup(repository => repository.AddAsync(It.IsAny<Article>()))
            .Callback<Article>(newArticle =>
                article = newArticle
            );
        _unitOfWorkMock.Setup(work => work.Commit())
            .ReturnsAsync(true);

        var result = await _sut.AddArticle(articleToInsert);

        _unitOfWorkMock.Verify(work => work.Commit(), Times.Once);
        _userRepositoryMock.Verify(repository => repository.GetUserWithArticlesAsync(It.IsAny<int>()), Times.Once);
        _articleRepositoryMock.Verify(repository => repository.AddAsync(It.IsAny<Article>()), Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        ((ObjectResult)result.Result).Value.Should().BeEquivalentTo(_mapper.Map<ArticleToDisplayDto>(article));
    }

    [Fact]
    public async Task UpdateArticle_ReturnsNotFound_WhenThereIsNoSuchArticle()
    {
        var userId = 5;
        var articleId = 10;
        var claim = new Claim("userId", userId.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _articleRepositoryMock.Setup(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()))
            .ReturnsAsync(() => null);

        var result = await _sut.UpdateArticle(articleId, new ArticleToUpsertDto());

        _articleRepositoryMock.Verify(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()),
            Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateArticle_ReturnsForbid_WhenTheUserIsNotTheAuthor()
    {
        var userId = 5;
        var articleId = 10;
        var claim = new Claim("userId", userId.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _articleRepositoryMock.Setup(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()))
            .ReturnsAsync(new Article { Author = new User { Id = 10 } });

        var result = await _sut.UpdateArticle(articleId, new ArticleToUpsertDto());

        _articleRepositoryMock.Verify(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()),
            Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateArticle_ReturnsNoContent_WhenTheArticleIsUpdated()
    {
        var userId = 5;
        var articleId = 10;
        var claim = new Claim("userId", userId.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _articleRepositoryMock.Setup(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()))
            .ReturnsAsync(new Article { Author = new User { Id = userId } });
        _unitOfWorkMock.Setup(work => work.Commit())
            .ReturnsAsync(true);

        var result = await _sut.UpdateArticle(articleId, new ArticleToUpsertDto());

        _unitOfWorkMock.Verify(work => work.Commit(), Times.Once);
        _articleRepositoryMock.Verify(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()),
            Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteArticle_ReturnsNotFound_WhenThereIsNoSuchArticle()
    {
        var userId = 5;
        var articleId = 10;
        var claim = new Claim("userId", userId.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _articleRepositoryMock.Setup(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()))
            .ReturnsAsync(() => null);

        var result = await _sut.DeleteArticle(articleId);

        _articleRepositoryMock.Verify(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()),
            Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_ReturnsForbid_WhenTheUserIsNotTheAuthor()
    {
        var userId = 5;
        var articleId = 10;
        var claim = new Claim("userId", userId.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _articleRepositoryMock.Setup(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()))
            .ReturnsAsync(new Article { Author = new User { Id = 10 } });

        var result = await _sut.DeleteArticle(articleId);

        _articleRepositoryMock.Verify(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()),
            Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DeleteArticle_ReturnsNoContent_WhenTheArticleIsDeleted()
    {
        var userId = 5;
        var articleId = 10;
        var claim = new Claim("userId", userId.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        var article = new Article
        {
            Id = articleId,
            Author = new User { Id = userId }
        };
        Article deletedArticle = null;
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _articleRepositoryMock.Setup(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()))
            .ReturnsAsync(article);
        _articleRepositoryMock.Setup(repository => repository.Remove(It.IsAny<Article>()))
            .Callback<Article>(articleToDelete =>
                deletedArticle = articleToDelete
            );
        _unitOfWorkMock.Setup(work => work.Commit())
            .ReturnsAsync(true);

        var result = await _sut.DeleteArticle(articleId);

        _unitOfWorkMock.Verify(work => work.Commit(), Times.Once);
        _articleRepositoryMock.Verify(repository => repository.Remove(It.IsAny<Article>()), Times.Once);
        _articleRepositoryMock.Verify(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()),
            Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<NoContentResult>();
        deletedArticle.Author.Id.Should().Be(userId);
        deletedArticle.Id.Should().Be(articleId);
    }

    [Fact]
    public async Task AddToFavorite_ReturnsNotFound_WhenThereIsNoSuchArticle()
    {
        var user = new User
        {
            Id = 5
        };
        var articleToFav = new Article
        {
            Id = 10
        };
        var claim = new Claim("userId", user.Id.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _userRepositoryMock.Setup(repository => repository.GetUserWithArticlesAsync(It.IsAny<int>()))
            .ReturnsAsync(user);
        _articleRepositoryMock.Setup(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()))
            .ReturnsAsync(() => null);

        var result = await _sut.AddToFavorite(articleToFav.Id);

        _userRepositoryMock.Verify(repository => repository.GetUserWithArticlesAsync(It.IsAny<int>()), Times.Once);
        _articleRepositoryMock.Verify(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()),
            Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddToFavorite_ReturnsNoContent_WhenArticleIsFavorited()
    {
        var user = new User
        {
            Id = 5
        };
        var articleToFav = new Article
        {
            Id = 10
        };
        var claim = new Claim("userId", user.Id.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        FavoriteArticle favoriteArticle = null;
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _userRepositoryMock.Setup(repository => repository.GetUserWithArticlesAsync(It.IsAny<int>()))
            .ReturnsAsync(user);
        _articleRepositoryMock.Setup(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()))
            .ReturnsAsync(articleToFav);
        _articleRepositoryMock.Setup(repository => repository.FavoriteArticle(It.IsAny<User>(), It.IsAny<Article>()))
            .Callback<User, Article>((user1, article) =>
                favoriteArticle = new FavoriteArticle
                {
                    User = user1,
                    UserId = user1.Id,
                    Article = article,
                    ArticleId = article.Id
                });
        _unitOfWorkMock.Setup(work => work.Commit())
            .ReturnsAsync(true);

        var result = await _sut.AddToFavorite(articleToFav.Id);

        _unitOfWorkMock.Verify(work => work.Commit(), Times.Once);
        _articleRepositoryMock.Verify(repository => repository.FavoriteArticle(It.IsAny<User>(), It.IsAny<Article>()),
            Times.Once);
        _articleRepositoryMock.Verify(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()),
            Times.Once);
        _userRepositoryMock.Verify(repository => repository.GetUserWithArticlesAsync(It.IsAny<int>()), Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<NoContentResult>();
        favoriteArticle.UserId.Should().Be(user.Id);
        favoriteArticle.ArticleId.Should().Be(articleToFav.Id);
    }

    [Fact]
    public async Task RemoveFromFavorite_ReturnsNotFound_WhenThereIsNoSuchArticle()
    {
        var user = new User
        {
            Id = 5
        };
        var articleToUnFav = new Article
        {
            Id = 10
        };
        var claim = new Claim("userId", user.Id.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _userRepositoryMock.Setup(repository => repository.GetUserWithArticlesAsync(It.IsAny<int>()))
            .ReturnsAsync(user);
        _articleRepositoryMock.Setup(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()))
            .ReturnsAsync(() => null);

        var result = await _sut.RemoveFromFavorite(articleToUnFav.Id);

        _userRepositoryMock.Verify(repository => repository.GetUserWithArticlesAsync(It.IsAny<int>()), Times.Once);
        _articleRepositoryMock.Verify(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()),
            Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RemoveFromFavorite_ReturnsNoContent_WhenArticleIsRemovedFromFavorite()
    {
        var user = new User
        {
            Id = 5
        };
        var articleToUnFav = new Article
        {
            Id = 10
        };
        var claim = new Claim("userId", user.Id.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        FavoriteArticle removedArticle = null;
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _userRepositoryMock.Setup(repository => repository.GetUserWithArticlesAsync(It.IsAny<int>()))
            .ReturnsAsync(user);
        _articleRepositoryMock.Setup(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()))
            .ReturnsAsync(articleToUnFav);
        _articleRepositoryMock.Setup(repository => repository.UnFavoriteArticle(It.IsAny<User>(), It.IsAny<Article>()))
            .Callback<User, Article>((user1, article) =>
                removedArticle = new FavoriteArticle
                {
                    User = user1,
                    UserId = user1.Id,
                    Article = article,
                    ArticleId = article.Id
                });
        _unitOfWorkMock.Setup(work => work.Commit())
            .ReturnsAsync(true);

        var result = await _sut.RemoveFromFavorite(articleToUnFav.Id);

        _unitOfWorkMock.Verify(work => work.Commit(), Times.Once);
        _articleRepositoryMock.Verify(repository => repository.UnFavoriteArticle(It.IsAny<User>(), It.IsAny<Article>()),
            Times.Once);
        _articleRepositoryMock.Verify(repository => repository.GetArticleWithoutCommentsAsync(It.IsAny<int>()),
            Times.Once);
        _userRepositoryMock.Verify(repository => repository.GetUserWithArticlesAsync(It.IsAny<int>()), Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<NoContentResult>();
        removedArticle.UserId.Should().Be(user.Id);
        removedArticle.ArticleId.Should().Be(articleToUnFav.Id);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    public async Task GetFeed_ReturnsThePaginatedArticlesWithOkResponse(int pageIndex, int pageSize)
    {
        var user = new User
        {
            Id = 5
        };
        var followingUsers = new List<User>
        {
            new()
            {
                Id = user.Id + 1
            },
            new()
            {
                Id = user.Id + 2
            }
        };
        var following = new List<UserToUser>
        {
            new()
            {
                User = followingUsers[0],
                UserId = followingUsers[0].Id,
                Follower = user,
                FollowerId = user.Id
            },
            new()
            {
                User = followingUsers[1],
                UserId = followingUsers[1].Id,
                Follower = user,
                FollowerId = user.Id
            }
        };
        followingUsers[0].Followers.Add(following[0]);
        followingUsers[1].Followers.Add(following[1]);
        user.Following = following;
        var articles = new List<Article>
        {
            new()
            {
                Author = following[0].User
            },
            new()
            {
                Author = following[1].User
            }
        };
        var claim = new Claim("userId", user.Id.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _userRepositoryMock.Setup(repository => repository.GetUserWithFollowAsync(It.IsAny<int>()))
            .ReturnsAsync(user);
        _articleRepositoryMock.Setup(repository =>
                repository.GetFeedAsync(It.IsAny<User>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(articles);

        var result = await _sut.GetFeed(pageIndex, pageSize);

        _articleRepositoryMock.Verify(
            repository => repository.GetFeedAsync(It.IsAny<User>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Once);
        _userRepositoryMock.Verify(repository => repository.GetUserWithFollowAsync(It.IsAny<int>()), Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Result.Should().BeOfType<OkObjectResult>();
        ((ObjectResult)result.Result).Value.Should().BeOfType<List<ArticleToDisplayDto>>();
        ((ObjectResult)result.Result).Value.Should()
            .BeEquivalentTo(_mapper.Map<IEnumerable<ArticleToDisplayDto>>(articles));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    public async Task GetFeed_ReturnsEmptyList_WhenUserHasNoFollowing(int pageIndex, int pageSize)
    {
        var user = new User
        {
            Id = 5,
            Following = null
        };

        var articles = new List<Article>();
        var claim = new Claim("userId", user.Id.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _userRepositoryMock.Setup(repository => repository.GetUserWithFollowAsync(It.IsAny<int>()))
            .ReturnsAsync(user);
        _articleRepositoryMock.Setup(repository =>
                repository.GetFeedAsync(It.IsAny<User>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(articles);

        var result = await _sut.GetFeed(pageIndex, pageSize);

        _articleRepositoryMock.Verify(
            repository => repository.GetFeedAsync(It.IsAny<User>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Once);
        _userRepositoryMock.Verify(repository => repository.GetUserWithFollowAsync(It.IsAny<int>()), Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Result.Should().BeOfType<OkObjectResult>();
        ((ObjectResult)result.Result).Value.Should().BeOfType<List<ArticleToDisplayDto>>();
        ((ObjectResult)result.Result).Value.Should()
            .BeEquivalentTo(_mapper.Map<IEnumerable<ArticleToDisplayDto>>(articles));
    }
}