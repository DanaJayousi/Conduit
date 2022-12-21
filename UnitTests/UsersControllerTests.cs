using System.Security.Claims;
using API.Controllers;
using API.Models;
using API.Profiles;
using AutoMapper;
using Domain.Interfaces;
using Domain.User;
using Domain.UserToUser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UnitTests;

public class UsersControllerTests
{
    private readonly Mock<HttpContext> _contextMock = new();

    private readonly Mapper _mapper = new(new MapperConfiguration(cfg =>
        cfg.AddProfile(new UserProfile())));

    private readonly UsersController _sut;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ClaimsPrincipal> _userMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();


    public UsersControllerTests()
    {
        var controllerContext = new ControllerContext
        {
            HttpContext = _contextMock.Object
        };
        _sut = new UsersController(_unitOfWorkMock.Object, _userRepositoryMock.Object, _mapper)
        {
            ControllerContext = controllerContext
        };
    }

    [Fact]
    public async Task GetUserById_ReturnsNotFound_WhenThereIsNoSuchUser()
    {
        _userRepositoryMock.Setup(repository => repository.GetUserWithFollowAsync(It.IsAny<int>()))
            .ReturnsAsync(() => null);

        var result = await _sut.GetUserById(0);

        _userRepositoryMock.Verify(repository => repository.GetUserWithFollowAsync(It.IsAny<int>()), Times.Once);
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetUserById_ReturnsTheUser_WhenUserExist()
    {
        var userFromDb = new User
        {
            Id = 5,
            Email = "email@valid",
            Password = "123456",
            FirstName = "FN",
            LastName = "LN"
        };
        var userAfterMapping = _mapper.Map<UserToDisplayDto>(userFromDb);
        _userRepositoryMock.Setup(repository => repository.GetUserWithFollowAsync(It.IsAny<int>()))
            .ReturnsAsync(userFromDb);

        var result = await _sut.GetUserById(5);

        _userRepositoryMock.Verify(repository => repository.GetUserWithFollowAsync(It.IsAny<int>()), Times.Once);
        result.Result.Should().BeOfType<OkObjectResult>();
        ((ObjectResult)result.Result).Value.Should().BeEquivalentTo(userAfterMapping);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnForbid_WhenCurrentUserIsNotTheUserToUpdate()
    {
        var userId = 5;
        var claim = new Claim("userId", userId.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);

        var result = await _sut.UpdateUser(userId + 1, new UserForUpsertDto());

        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        var userId = 5;
        var claim = new Claim("userId", userId.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _userRepositoryMock.Setup(repository => repository.GetAsync(It.IsAny<int>()))
            .ReturnsAsync(() => null);

        var result = await _sut.UpdateUser(userId, new UserForUpsertDto());

        _userRepositoryMock.Verify(repository => repository.GetAsync(It.IsAny<int>()), Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnConflict_WhenUserUsesAnAlreadyUsedEmail()
    {
        var userId = 5;
        var userToUpdate = new UserForUpsertDto
        {
            Email = "email@valid",
            Password = "123456",
            FirstName = "FN",
            LastName = "LN"
        };
        var claim = new Claim("userId", userId.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _userRepositoryMock.Setup(repository => repository.GetAsync(It.IsAny<int>()))
            .ReturnsAsync(new User());
        _userRepositoryMock.Setup(repository => repository.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new User { Id = userId + 1 });

        var result = await _sut.UpdateUser(userId, userToUpdate);

        _userRepositoryMock.Verify(repository => repository.GetUserByEmailAsync(It.IsAny<string>()), Times.Once);
        _userRepositoryMock.Verify(repository => repository.GetAsync(It.IsAny<int>()), Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<ConflictResult>();
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnNoContent_WhenThereIsNoEmailConflict()
    {
        var userId = 5;
        var userToUpdate = new UserForUpsertDto
        {
            Email = "email@valid",
            Password = "123456",
            FirstName = "FN",
            LastName = "LN"
        };
        var claim = new Claim("userId", userId.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _userRepositoryMock.Setup(repository => repository.GetAsync(It.IsAny<int>()))
            .ReturnsAsync(new User());
        _userRepositoryMock.Setup(repository => repository.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(() => null);
        _unitOfWorkMock.Setup(work => work.Commit())
            .ReturnsAsync(true);

        var result = await _sut.UpdateUser(userId, userToUpdate);

        _unitOfWorkMock.Verify(work => work.Commit(), Times.Once);
        _userRepositoryMock.Verify(repository => repository.GetUserByEmailAsync(It.IsAny<string>()), Times.Once);
        _userRepositoryMock.Verify(repository => repository.GetAsync(It.IsAny<int>()), Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnNoContent_WhenThereIsNoEmailChange()
    {
        var userId = 5;
        var userToUpdate = new UserForUpsertDto
        {
            Email = "email@valid",
            Password = "123456",
            FirstName = "FN",
            LastName = "LN"
        };
        var claim = new Claim("userId", userId.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _userRepositoryMock.Setup(repository => repository.GetAsync(It.IsAny<int>()))
            .ReturnsAsync(new User());
        _userRepositoryMock.Setup(repository => repository.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new User { Id = userId });
        _unitOfWorkMock.Setup(work => work.Commit())
            .ReturnsAsync(true);

        var result = await _sut.UpdateUser(userId, userToUpdate);

        _unitOfWorkMock.Verify(work => work.Commit(), Times.Once);
        _userRepositoryMock.Verify(repository => repository.GetUserByEmailAsync(It.IsAny<string>()), Times.Once);
        _userRepositoryMock.Verify(repository => repository.GetAsync(It.IsAny<int>()), Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Follow_ShouldReturnForbid_WhenCurrentUserIsNotTheFollower()
    {
        var userId = 5;
        var claim = new Claim("userId", userId.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);

        var result = await _sut.Follow(userId + 1, userId + 2);

        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Follow_ShouldReturnBadRequest_WhenTheFollowerIsTheFollowing()
    {
        var userId = 5;
        var claim = new Claim("userId", userId.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);

        var result = await _sut.Follow(userId, userId);

        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task Follow_ShouldReturnNotFound_WhenTheFollowerDoesNotExist()
    {
        var userId = 5;
        var claim = new Claim("userId", userId.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _userRepositoryMock.Setup(repository => repository.GetUserWithFollowAsync(It.IsAny<int>()))
            .ReturnsAsync(() => null);

        var result = await _sut.Follow(userId, userId + 1);

        _userRepositoryMock.Verify(repository => repository.GetUserWithFollowAsync(It.IsAny<int>()), Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Follow_ShouldReturnNotFound_WhenTheFollowingDoesNotExist()
    {
        var userId = 5;
        var claim = new Claim("userId", userId.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _userRepositoryMock.SetupSequence(repository => repository.GetUserWithFollowAsync(It.IsAny<int>()))
            .ReturnsAsync(new User { Id = userId })
            .ReturnsAsync(() => null);

        var result = await _sut.Follow(userId, userId + 1);

        _userRepositoryMock.Verify(repository => repository.GetUserWithFollowAsync(It.IsAny<int>()), Times.Exactly(2));
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Follow_ShouldReturnNoContent_WhenFollowSucceeds()
    {
        var userId = 5;
        var claim = new Claim("userId", userId.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        UserToUser link = null;
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _userRepositoryMock.SetupSequence(repository => repository.GetUserWithFollowAsync(It.IsAny<int>()))
            .ReturnsAsync(new User { Id = userId })
            .ReturnsAsync(new User { Id = userId + 1 });
        _userRepositoryMock.Setup(repository => repository.Follow(It.IsAny<User>(), It.IsAny<User>()))
            .Callback<User, User>((user, follower) =>
                link = new UserToUser
                {
                    User = user,
                    UserId = user.Id,
                    Follower = follower,
                    FollowerId = follower.Id
                });

        var result = await _sut.Follow(userId, userId + 1);

        _unitOfWorkMock.Verify(work => work.Commit(), Times.Once);
        _userRepositoryMock.Verify(repository => repository.Follow(It.IsAny<User>(), It.IsAny<User>()), Times.Once);
        _userRepositoryMock.Verify(repository => repository.GetUserWithFollowAsync(It.IsAny<int>()), Times.Exactly(2));
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<NoContentResult>();
        link.UserId.Should().Be(userId + 1);
        link.FollowerId.Should().Be(userId);
    }

    [Fact]
    public async Task Unfollow_ShouldReturnForbid_WhenCurrentUserIsNotTheUnfollower()
    {
        var userId = 5;
        var claim = new Claim("userId", userId.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);

        var result = await _sut.Unfollow(userId + 1, userId + 2);

        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Unfollow_ShouldReturnBadRequest_WhenTheUnfollowerIsTheUnfollowed()
    {
        var userId = 5;
        var claim = new Claim("userId", userId.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);

        var result = await _sut.Unfollow(userId, userId);

        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task Unfollow_ShouldReturnNotFound_WhenTheUnfollowerDoesNotExist()
    {
        var userId = 5;
        var claim = new Claim("userId", userId.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _userRepositoryMock.Setup(repository => repository.GetUserWithFollowAsync(It.IsAny<int>()))
            .ReturnsAsync(() => null);

        var result = await _sut.Unfollow(userId, userId + 1);

        _userRepositoryMock.Verify(repository => repository.GetUserWithFollowAsync(It.IsAny<int>()), Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Unfollow_ShouldReturnNotFound_WhenTheUnfollowedDoesNotExist()
    {
        var userId = 5;
        var claim = new Claim("userId", userId.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _userRepositoryMock.SetupSequence(repository => repository.GetUserWithFollowAsync(It.IsAny<int>()))
            .ReturnsAsync(new User { Id = userId })
            .ReturnsAsync(() => null);

        var result = await _sut.Unfollow(userId, userId + 1);

        _userRepositoryMock.Verify(repository => repository.GetUserWithFollowAsync(It.IsAny<int>()), Times.Exactly(2));
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Unfollow_ShouldReturnNoContent_WhenUnfollowSucceeds()
    {
        var userId = 5;
        var claim = new Claim("userId", userId.ToString());
        var claims = new List<Claim>
        {
            claim
        };
        UserToUser link = null;
        _userMock.SetupGet(p => p.Claims)
            .Returns(claims);
        _contextMock.Setup(ctx => ctx.User)
            .Returns(_userMock.Object);
        _userRepositoryMock.SetupSequence(repository => repository.GetUserWithFollowAsync(It.IsAny<int>()))
            .ReturnsAsync(new User { Id = userId })
            .ReturnsAsync(new User { Id = userId + 1 });
        _userRepositoryMock.Setup(repository => repository.Unfollow(It.IsAny<User>(), It.IsAny<User>()))
            .Callback<User, User>((user, follower) =>
                link = new UserToUser
                {
                    User = user,
                    UserId = user.Id,
                    Follower = follower,
                    FollowerId = follower.Id
                });

        var result = await _sut.Unfollow(userId, userId + 1);

        _unitOfWorkMock.Verify(work => work.Commit(), Times.Once);
        _userRepositoryMock.Verify(repository => repository.Unfollow(It.IsAny<User>(), It.IsAny<User>()), Times.Once);
        _userRepositoryMock.Verify(repository => repository.GetUserWithFollowAsync(It.IsAny<int>()), Times.Exactly(2));
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<NoContentResult>();
        link.UserId.Should().Be(userId + 1);
        link.FollowerId.Should().Be(userId);
    }
}