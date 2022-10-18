using System.Security.Claims;
using API.Controllers;
using API.Models;
using API.Profiles;
using API.Services;
using AutoMapper;
using Domain.Interfaces;
using Domain.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace UnitTests;

public class AuthenticationControllerTests
{
    private readonly Dictionary<string, string> _configForAuthentication = new()
    {
        { "Authentication:SecretForKey", "thisIsAVeryStrongSecretForKey" },
        { "Authentication:Issuer", "https://localhost:7271" },
        { "Authentication:Audience", "ConduitAPI" }
    };

    private readonly Mock<HttpContext> _contextMock = new();

    private readonly Mapper _mapper = new(new MapperConfiguration(cfg =>
        cfg.AddProfile(new UserProfile())));

    private readonly AuthenticationController _sut;
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ClaimsPrincipal> _userMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();

    public AuthenticationControllerTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(_configForAuthentication)
            .Build();
        var controllerContext = new ControllerContext
        {
            HttpContext = _contextMock.Object
        };
        _sut = new AuthenticationController(_unitOfWorkMock.Object,
            _userRepositoryMock.Object, _mapper, configuration,
            _tokenServiceMock.Object)
        {
            ControllerContext = controllerContext
        };
    }

    [Fact]
    public async Task SignIn_ReturnsUnauthorized_WhenUserCredentialsAreInvalid_UserEmailDoesNotExist()
    {
        var authenticationDto = new AuthenticationDto
        {
            Email = "email@email",
            Password = "password"
        };
        _userRepositoryMock.Setup(repository => repository.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(() => null);

        var result = await _sut.SignIn(authenticationDto);

        _userRepositoryMock.Verify(
            repository => repository.ValidateUserCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task SignIn_ReturnsUnauthorized_WhenUserCredentialsAreInvalid_UserPasswordIncorrect()
    {
        var authenticationDto = new AuthenticationDto
        {
            Email = "email@email",
            Password = "password"
        };
        var user = new User
        {
            Password = "notTheSamePassword"
        };
        string emailString = null;
        _userRepositoryMock.Setup(repository => repository.GetUserByEmailAsync(It.IsAny<string>()))
            .Callback<string>(s => emailString = s)
            .ReturnsAsync(user);

        var result = await _sut.SignIn(authenticationDto);

        _userRepositoryMock.Verify(
            repository => repository.ValidateUserCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        result.Result.Should().BeOfType<UnauthorizedResult>();
        emailString.Should().BeEquivalentTo(user.Email);
    }

    [Fact]
    public async Task SignIn_ReturnsTheTokens()
    {
        var authenticationDto = new AuthenticationDto
        {
            Email = "email@email",
            Password = "password"
        };
        var user = new User
        {
            Id = 1,
            Email = "email@email",
            Password = "password"
        };
        var claimsForToken = new List<Claim> { new("userId", user.Id.ToString()) };
        List<Claim> addedClaims = null;
        string addedSecretKey = null;
        string addedIssuer = null;
        string addedAudience = null;
        var token = new Token
        {
            accessToken = "someAccessToken",
            refreshToken = "someRefreshToken"
        };
        var expDate = new DateTime();
        _userRepositoryMock.Setup(repository =>
                repository.ValidateUserCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(user);
        _tokenServiceMock.Setup(service => service.GenerateAccessToken(It.IsAny<List<Claim>>(), It.IsAny<string>()
                , It.IsAny<string>(), It.IsAny<string>()))
            .Callback<List<Claim>, string, string, string>((claims, secretKey, issuer, audience) =>
                {
                    addedClaims = claims;
                    addedSecretKey = secretKey;
                    addedIssuer = issuer;
                    addedAudience = audience;
                }
            )
            .Returns(token.accessToken);
        _tokenServiceMock.Setup(service => service.GenerateRefreshToken())
            .Callback(() => expDate = DateTime.UtcNow.AddDays(10))
            .Returns(token.refreshToken);
        _unitOfWorkMock.Setup(work => work.Commit())
            .ReturnsAsync(true);

        var result = await _sut.SignIn(authenticationDto);

        _unitOfWorkMock.Verify(work => work.Commit(), Times.Once);
        _tokenServiceMock.Verify(service => service.GenerateAccessToken(It.IsAny<List<Claim>>(), It.IsAny<string>()
            , It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _tokenServiceMock.Verify(service => service.GenerateRefreshToken(), Times.Once);
        _userRepositoryMock.Verify(repository =>
                repository.ValidateUserCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
        result.Result.Should().BeOfType<OkObjectResult>();
        ((ObjectResult)result.Result).Value.Should().BeEquivalentTo(token);
        user.RefreshToken.Should().BeEquivalentTo(token.refreshToken);
        user.RefreshTokenExpiryTime.Should().BeCloseTo(expDate, TimeSpan.FromSeconds(1));
        addedClaims.Should().BeEquivalentTo(claimsForToken);
        addedSecretKey.Should().BeEquivalentTo(_configForAuthentication["Authentication:SecretForKey"]);
        addedIssuer.Should().BeEquivalentTo(_configForAuthentication["Authentication:Issuer"]);
        addedAudience.Should().BeEquivalentTo(_configForAuthentication["Authentication:Audience"]);
    }

    [Fact]
    public async Task SignUp_ReturnsConflict_WhenTheEmailIsAlreadyUsed()
    {
        var user = new UserForUpsertDto
        {
            Email = "email@used"
        };
        string email = null;
        _userRepositoryMock.Setup(repository => repository.GetUserByEmailAsync(It.IsAny<string>()))
            .Callback<string>(s => email = s)
            .ReturnsAsync(new User());

        var result = await _sut.SignUp(user);

        _userRepositoryMock.Verify(repository => repository.GetUserByEmailAsync(It.IsAny<string>()), Times.Once);
        result.Result.Should().BeOfType<ConflictResult>();
        email.Should().BeEquivalentTo(user.Email);
    }

    [Fact]
    public async Task SignUp_ReturnsTheAddedUser()
    {
        var userForUpsertDto = new UserForUpsertDto
        {
            Email = "email@used"
        };
        User addedUser = null;
        string email = null;
        _userRepositoryMock.Setup(repository => repository.GetUserByEmailAsync(It.IsAny<string>()))
            .Callback<string>(s => email = s)
            .ReturnsAsync(() => null);
        _userRepositoryMock.Setup(repository => repository.AddAsync(It.IsAny<User>()))
            .Callback<User>(user => addedUser = user);
        _unitOfWorkMock.Setup(work => work.Commit())
            .ReturnsAsync(true);

        var result = await _sut.SignUp(userForUpsertDto);

        _unitOfWorkMock.Verify(work => work.Commit(), Times.Once);
        _userRepositoryMock.Verify(repository => repository.AddAsync(It.IsAny<User>()), Times.Once);
        _userRepositoryMock.Verify(repository => repository.GetUserByEmailAsync(It.IsAny<string>()), Times.Once);
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        ((ObjectResult)result.Result).Value.Should().BeEquivalentTo(_mapper.Map<UserToDisplayDto>(addedUser));
        email.Should().BeEquivalentTo(userForUpsertDto.Email);
    }

    [Fact]
    public async Task Refresh_ReturnsBadRequest_WhenTokenIsNull()
    {
        var result = await _sut.Refresh(null);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Refresh_ReturnsBadRequest_WhenThereIsNoSuchUser()
    {
        var token = new Token
        {
            accessToken = "accessToken",
            refreshToken = "refreshToken"
        };
        string invokedAccessToken = null;
        string invokedSecretKey = null;
        _tokenServiceMock.Setup(service => service.GetUserIdFromAccessToken(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((access, secret) =>
            {
                invokedAccessToken = access;
                invokedSecretKey = secret;
            })
            .Returns(0);
        _userRepositoryMock.Setup(repository => repository.GetAsync(It.IsAny<int>()))
            .ReturnsAsync(() => null);

        var result = await _sut.Refresh(token);

        _userRepositoryMock.Verify(repository => repository.GetAsync(It.IsAny<int>()), Times.Once);
        _tokenServiceMock.Verify(service => service.GetUserIdFromAccessToken(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        invokedAccessToken.Should().BeEquivalentTo(token.accessToken);
        invokedSecretKey.Should().BeEquivalentTo(_configForAuthentication["Authentication:SecretForKey"]);
    }

    [Fact]
    public async Task Refresh_ReturnsBadRequest_WhenRefreshTokensMismatch()
    {
        var token = new Token
        {
            accessToken = "accessToken",
            refreshToken = "refreshToken"
        };
        var user = new User
        {
            Id = 1,
            RefreshToken = "notTheSameToken"
        };
        string invokedAccessToken = null;
        string invokedSecretKey = null;
        _tokenServiceMock.Setup(service => service.GetUserIdFromAccessToken(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((access, secret) =>
            {
                invokedAccessToken = access;
                invokedSecretKey = secret;
            })
            .Returns(user.Id);
        _userRepositoryMock.Setup(repository => repository.GetAsync(It.IsAny<int>()))
            .ReturnsAsync(user);

        var result = await _sut.Refresh(token);

        _userRepositoryMock.Verify(repository => repository.GetAsync(It.IsAny<int>()), Times.Once);
        _tokenServiceMock.Verify(service => service.GetUserIdFromAccessToken(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        invokedAccessToken.Should().BeEquivalentTo(token.accessToken);
        invokedSecretKey.Should().BeEquivalentTo(_configForAuthentication["Authentication:SecretForKey"]);
    }

    [Fact]
    public async Task Refresh_ReturnsBadRequest_WhenRefreshTokenExpired()
    {
        var token = new Token
        {
            accessToken = "accessToken",
            refreshToken = "refreshToken"
        };
        var user = new User
        {
            Id = 1,
            RefreshToken = "refreshToken",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1)
        };
        string invokedAccessToken = null;
        string invokedSecretKey = null;
        _tokenServiceMock.Setup(service => service.GetUserIdFromAccessToken(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((access, secret) =>
            {
                invokedAccessToken = access;
                invokedSecretKey = secret;
            })
            .Returns(user.Id);
        _userRepositoryMock.Setup(repository => repository.GetAsync(It.IsAny<int>()))
            .ReturnsAsync(user);

        var result = await _sut.Refresh(token);

        _userRepositoryMock.Verify(repository => repository.GetAsync(It.IsAny<int>()), Times.Once);
        _tokenServiceMock.Verify(service => service.GetUserIdFromAccessToken(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        invokedAccessToken.Should().BeEquivalentTo(token.accessToken);
        invokedSecretKey.Should().BeEquivalentTo(_configForAuthentication["Authentication:SecretForKey"]);
    }

    [Fact]
    public async Task Refresh_ReturnsToken()
    {
        var token = new Token
        {
            accessToken = "accessToken",
            refreshToken = "refreshToken"
        };
        var newToken = new Token
        {
            accessToken = "newAccessToken",
            refreshToken = "newRefreshToken"
        };
        var user = new User
        {
            Id = 1,
            RefreshToken = "refreshToken",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
        };
        var claimsForToken = new List<Claim> { new("userId", user.Id.ToString()) };
        string invokedAccessToken = null;
        string invokedSecretKey = null;
        List<Claim> invokedClaimsInTokenGenerator = null;
        string invokedIssuerInTokenGenerator = null;
        string invokedAudienceInTokenGenerator = null;
        string invokedSecretKeyInTokenGenerator = null;
        _tokenServiceMock.Setup(service => service.GetUserIdFromAccessToken(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((access, secret) =>
            {
                invokedAccessToken = access;
                invokedSecretKey = secret;
            })
            .Returns(user.Id);
        _userRepositoryMock.Setup(repository => repository.GetAsync(It.IsAny<int>()))
            .ReturnsAsync(user);
        _tokenServiceMock.Setup(service => service.GenerateAccessToken(It.IsAny<List<Claim>>(), It.IsAny<string>()
                , It.IsAny<string>(), It.IsAny<string>()))
            .Callback<List<Claim>, string, string, string>((claims, secretKey, issuer, audience) =>
                {
                    invokedClaimsInTokenGenerator = claims;
                    invokedSecretKeyInTokenGenerator = secretKey;
                    invokedIssuerInTokenGenerator = issuer;
                    invokedAudienceInTokenGenerator = audience;
                }
            )
            .Returns(newToken.accessToken);
        _tokenServiceMock.Setup(service => service.GenerateRefreshToken())
            .Returns(newToken.refreshToken);
        _unitOfWorkMock.Setup(work => work.Commit())
            .ReturnsAsync(true);

        var result = await _sut.Refresh(token);

        _unitOfWorkMock.Verify(work => work.Commit(), Times.Once);
        _tokenServiceMock.Verify(service => service.GenerateAccessToken(It.IsAny<List<Claim>>(), It.IsAny<string>()
            , It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _tokenServiceMock.Verify(service => service.GenerateRefreshToken(), Times.Once);
        _userRepositoryMock.Verify(repository => repository.GetAsync(It.IsAny<int>()), Times.Once);
        _tokenServiceMock.Verify(service => service.GetUserIdFromAccessToken(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
        result.Result.Should().BeOfType<OkObjectResult>();
        ((ObjectResult)result.Result).Value.Should().BeEquivalentTo(newToken);
        invokedAccessToken.Should().BeEquivalentTo(token.accessToken);
        invokedSecretKey.Should().BeEquivalentTo(_configForAuthentication["Authentication:SecretForKey"]);
        user.RefreshToken.Should().BeEquivalentTo(newToken.refreshToken);
        invokedClaimsInTokenGenerator.Should().BeEquivalentTo(claimsForToken);
        invokedSecretKeyInTokenGenerator.Should()
            .BeEquivalentTo(_configForAuthentication["Authentication:SecretForKey"]);
        invokedIssuerInTokenGenerator.Should().BeEquivalentTo(_configForAuthentication["Authentication:Issuer"]);
        invokedAudienceInTokenGenerator.Should().BeEquivalentTo(_configForAuthentication["Authentication:Audience"]);
    }

    [Fact]
    public async Task Logout_ReturnsBadRequest_WhenThereIsNoSuchUser()
    {
        var userId = 1;
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

        var result = await _sut.Logout();

        _userRepositoryMock.Verify(repository => repository.GetAsync(It.IsAny<int>()), Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task Logout_ReturnsNoContent()
    {
        var user = new User
        {
            Id = 1,
            RefreshToken = "refreshToken",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
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
        _userRepositoryMock.Setup(repository => repository.GetAsync(It.IsAny<int>()))
            .ReturnsAsync(user);
        _unitOfWorkMock.Setup(work => work.Commit())
            .ReturnsAsync(true);

        var result = await _sut.Logout();

        _unitOfWorkMock.Verify(work => work.Commit(), Times.Once);
        _userRepositoryMock.Verify(repository => repository.GetAsync(It.IsAny<int>()), Times.Once);
        _userMock.VerifyGet(p => p.Claims, Times.Once);
        result.Should().BeOfType<NoContentResult>();
        user.RefreshToken.Should().BeEmpty();
    }
}