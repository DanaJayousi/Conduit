using System.Security.Claims;
using API.Controllers;
using API.Models;
using API.Profiles;
using AutoMapper;
using Domain.Article;
using Domain.Comment;
using Domain.Interfaces;
using Domain.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UnitTests;

public class CommentsControllerTests
{
    private readonly Mock<IArticleRepository> _articleRepositoryMock = new();
    private readonly Mock<HttpContext> _contextMock = new();

    private readonly Mapper _mapper = new(new MapperConfiguration(cfg =>
        cfg.AddProfile(new CommentProfile())));

    private readonly CommentsController _sut;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ClaimsPrincipal> _userMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();

    public CommentsControllerTests()
    {
        var controllerContext = new ControllerContext
        {
            HttpContext = _contextMock.Object
        };
        _sut = new CommentsController(_mapper, _unitOfWorkMock.Object, _articleRepositoryMock.Object,
            _userRepositoryMock.Object)
        {
            ControllerContext = controllerContext
        };
    }

    [Fact]
    public async Task GetCommentById_ReturnsNotFound_WhenThereIsNoSuchArticle()
    {
        _articleRepositoryMock.Setup(repository => repository.GetArticleWithCommentsAsync(It.IsAny<int>()))
            .ReturnsAsync(() => null);

        var result = await _sut.GetCommentById(1, 1);

        _articleRepositoryMock.Verify(repository => repository.GetArticleWithCommentsAsync(It.IsAny<int>()),
            Times.Once);
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetCommentById_ReturnsNotFound_WhenThereIsNoSuchComment()
    {
        var articleId = 1;
        _articleRepositoryMock.Setup(repository => repository.GetArticleWithCommentsAsync(It.IsAny<int>()))
            .ReturnsAsync(new Article { Id = articleId });
        _articleRepositoryMock.Setup(repository => repository.GetCommentByIdAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(() => null);

        var result = await _sut.GetCommentById(articleId, 1);

        _articleRepositoryMock.Verify(repository => repository.GetArticleWithCommentsAsync(It.IsAny<int>()),
            Times.Once);
        _articleRepositoryMock.Verify(repository => repository.GetCommentByIdAsync(It.IsAny<int>(), It.IsAny<int>()),
            Times.Once);
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetCommentById_ReturnsTheComment()
    {
        var article = new Article();
        var articleComment = new Comment();
        _articleRepositoryMock.Setup(repository => repository.GetArticleWithCommentsAsync(It.IsAny<int>()))
            .ReturnsAsync(new Article { Id = article.Id });
        _articleRepositoryMock.Setup(repository => repository.GetCommentByIdAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(articleComment);

        var result = await _sut.GetCommentById(article.Id, articleComment.Id);

        _articleRepositoryMock.Verify(repository => repository.GetArticleWithCommentsAsync(It.IsAny<int>()),
            Times.Once);
        _articleRepositoryMock.Verify(repository => repository.GetCommentByIdAsync(It.IsAny<int>(), It.IsAny<int>()),
            Times.Once);
        result.Result.Should().BeOfType<OkObjectResult>();
        ((ObjectResult)result.Result).Value.Should().BeEquivalentTo(_mapper.Map<CommentToDisplayDto>(articleComment));
    }

    [Fact]
    public async Task GetComments_ReturnsNotFound_WhenThereIsNoSuchArticle()
    {
        _articleRepositoryMock.Setup(repository => repository.GetArticleWithCommentsAsync(It.IsAny<int>()))
            .ReturnsAsync(() => null);

        var result = await _sut.GetAllComments(1);

        _articleRepositoryMock.Verify(repository => repository.GetArticleWithCommentsAsync(It.IsAny<int>()),
            Times.Once);
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetComments_ReturnsEmptyList_WhenThereIsNoComments()
    {
        var article = new Article();
        _articleRepositoryMock.Setup(repository => repository.GetArticleWithCommentsAsync(It.IsAny<int>()))
            .ReturnsAsync(article);
        _articleRepositoryMock.Setup(repository => repository.GetCommentsAsync(It.IsAny<int>()))
            .ReturnsAsync(() => null);

        var result = await _sut.GetAllComments(article.Id);

        _articleRepositoryMock.Verify(repository => repository.GetArticleWithCommentsAsync(It.IsAny<int>()),
            Times.Once);
        _articleRepositoryMock.Verify(repository => repository.GetCommentsAsync(It.IsAny<int>()), Times.Once);
        result.Result.Should().BeOfType<OkObjectResult>();
        ((ObjectResult)result.Result).Value.Should()
            .BeEquivalentTo(_mapper.Map<IEnumerable<CommentToDisplayDto>>(null));
    }

    [Fact]
    public async Task GetComments_ReturnsTheComments()
    {
        var article = new Article();
        var comments = new List<Comment>
        {
            new(),
            new()
        };
        _articleRepositoryMock.Setup(repository => repository.GetArticleWithCommentsAsync(It.IsAny<int>()))
            .ReturnsAsync(article);
        _articleRepositoryMock.Setup(repository => repository.GetCommentsAsync(It.IsAny<int>()))
            .ReturnsAsync(comments);

        var result = await _sut.GetAllComments(article.Id);

        _articleRepositoryMock.Verify(repository => repository.GetArticleWithCommentsAsync(It.IsAny<int>()),
            Times.Once);
        _articleRepositoryMock.Verify(repository => repository.GetCommentsAsync(It.IsAny<int>()), Times.Once);
        result.Result.Should().BeOfType<OkObjectResult>();
        ((ObjectResult)result.Result).Value.Should()
            .BeEquivalentTo(_mapper.Map<IEnumerable<CommentToDisplayDto>>(comments));
    }

    [Fact]
    public async Task AddComment_ReturnsNotFound_WhenThereIsNoSuchArticle()
    {
        var user = new User
        {
            Id = 5
        };
        var article = new Article
        {
            Id = 10
        };
        var claim = new Claim("userId", user.Id.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        var commentToInsert = new CommentToInsertDto
        {
            content = "content"
        };
        var insertedComment = new Comment
        {
            Author = user,
            Article = article,
            PublishDate = DateTime.UtcNow,
            Content = "content"
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _userRepositoryMock.Setup(repository => repository.GetUserWithArticlesAsync(It.IsAny<int>()))
            .ReturnsAsync(user);
        _articleRepositoryMock.Setup(repository => repository.GetArticleWithCommentsAsync(It.IsAny<int>()))
            .ReturnsAsync(article);
        _unitOfWorkMock.Setup(work => work.Commit())
            .ReturnsAsync(true);

        var result = await _sut.AddComment(5, commentToInsert);

        _unitOfWorkMock.Verify(work => work.Commit(), Times.Once);
        _userRepositoryMock.Verify(repository => repository.GetUserWithArticlesAsync(It.IsAny<int>()), Times.Once);
        _articleRepositoryMock.Verify(repository => repository.GetArticleWithCommentsAsync(It.IsAny<int>()),
            Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        ((ObjectResult)result.Result).Value.Should().BeEquivalentTo(_mapper.Map<CommentToDisplayDto>(insertedComment));
    }

    [Fact]
    public async Task DeleteComment_ReturnsNotFound_WhenThereIsNoSuchArticle()
    {
        var user = new User
        {
            Id = 5
        };
        var article = new Article
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
        _articleRepositoryMock.Setup(repository => repository.GetAsync(It.IsAny<int>()))
            .ReturnsAsync(() => null);

        var result = await _sut.DeleteComment(5, 1);

        _articleRepositoryMock.Verify(repository => repository.GetAsync(It.IsAny<int>()),
            Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteComment_ReturnsNotFound_WhenThereIsNoSuchComment()
    {
        var user = new User
        {
            Id = 5
        };
        var article = new Article
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
        _articleRepositoryMock.Setup(repository => repository.GetAsync(It.IsAny<int>()))
            .ReturnsAsync(article);
        _articleRepositoryMock.Setup(repository => repository.GetCommentByIdAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(() => null);

        var result = await _sut.DeleteComment(article.Id, 1);

        _articleRepositoryMock.Verify(repository => repository.GetCommentByIdAsync(It.IsAny<int>(), It.IsAny<int>()),
            Times.Once);
        _articleRepositoryMock.Verify(repository => repository.GetAsync(It.IsAny<int>()),
            Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteComment_ReturnsForbid_WhenTheUserIsNoTheCommentAuthor()
    {
        var user = new User
        {
            Id = 5
        };
        var article = new Article
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
        _articleRepositoryMock.Setup(repository => repository.GetAsync(It.IsAny<int>()))
            .ReturnsAsync(article);
        _articleRepositoryMock.Setup(repository => repository.GetCommentByIdAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new Comment { Author = new User { Id = 1 } });

        var result = await _sut.DeleteComment(article.Id, 1);

        _articleRepositoryMock.Verify(repository => repository.GetCommentByIdAsync(It.IsAny<int>(), It.IsAny<int>()),
            Times.Once);
        _articleRepositoryMock.Verify(repository => repository.GetAsync(It.IsAny<int>()),
            Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DeleteComment_DeletesTheComment()
    {
        var user = new User
        {
            Id = 5
        };
        var commentToBeDeleted = new Comment
        {
            Id = 1,
            Author = user,
            AuthorId = user.Id,
            Content = "no content",
            PublishDate = default
        };
        var article = new Article
        {
            Id = 10,
            Comments = new List<Comment> { commentToBeDeleted }
        };
        commentToBeDeleted.Article = article;
        commentToBeDeleted.ArticleId = article.Id;
        Comment deletedComment = null;
        var claim = new Claim("userId", user.Id.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _articleRepositoryMock.Setup(repository => repository.GetAsync(It.IsAny<int>()))
            .ReturnsAsync(article);
        _articleRepositoryMock.Setup(repository => repository.GetCommentByIdAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(commentToBeDeleted);
        _articleRepositoryMock.Setup(repository => repository.DeleteComment(It.IsAny<Comment>()))
            .Callback<Comment>(comment =>
                deletedComment = comment
            );
        _unitOfWorkMock.Setup(work => work.Commit()).ReturnsAsync(true);

        var result = await _sut.DeleteComment(article.Id, commentToBeDeleted.Id);

        _unitOfWorkMock.Verify(work => work.Commit(), Times.Once);
        _articleRepositoryMock.Verify(repository => repository.DeleteComment(It.IsAny<Comment>()),
            Times.Once);
        _articleRepositoryMock.Verify(repository => repository.GetCommentByIdAsync(It.IsAny<int>(), It.IsAny<int>()),
            Times.Once);
        _articleRepositoryMock.Verify(repository => repository.GetAsync(It.IsAny<int>()),
            Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<NoContentResult>();
        deletedComment.Should().BeEquivalentTo(commentToBeDeleted);
    }
}